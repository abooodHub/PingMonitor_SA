using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PingMonitor
{
    public sealed class ServerDefinition
    {
        public ServerDefinition(string countryCode, string locationAr, string providerName, string host)
        {
            CountryCode = (countryCode ?? string.Empty).Trim().ToLowerInvariant();
            LocationAr = (locationAr ?? string.Empty).Trim();
            ProviderName = (providerName ?? string.Empty).Trim();
            Host = (host ?? string.Empty).Trim();
            ProviderId = CreateProviderId(ProviderName);
            RegionId = CreateRegionId(CountryCode);
            Id = CreateStableId(ProviderId, CountryCode, LocationAr);
        }

        public string Id { get; private set; }
        public string CountryCode { get; private set; }
        public string LocationAr { get; private set; }
        public string ProviderName { get; private set; }
        public string ProviderId { get; private set; }
        public string RegionId { get; private set; }
        public string Host { get; private set; }

        public static string CreateProviderId(string providerName)
        {
            string normalized = (providerName ?? string.Empty).Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "amazon": return "amazon";
                case "google cloud": return "google-cloud";
                case "stc": return "stc";
                case "mobily": return "mobily";
                case "zain": return "zain";
                case "go telecom": return "go-telecom";
                case "salam": return "salam";
                case "etisalat": return "etisalat-ae";
                case "du": return "du-ae";
                case "stc bahrain": return "stc-bh";
                case "zain bahrain": return "zain-bh";
                case "etisalcom": return "etisalcom-bh";
                default: return normalized.Replace(' ', '-');
            }
        }

        public static string CreateRegionId(string countryCode)
        {
            string code = (countryCode ?? string.Empty).Trim().ToLowerInvariant();
            if (code == "sa") return "saudi-arabia";
            if (code == "ae" || code == "bh" || code == "qa") return "gulf";
            if (new[] { "de", "fr", "gb", "es", "ie", "it", "ch", "se", "nl", "no", "pl" }.Contains(code))
                return "europe";
            if (code == "i_n") return "asia";
            return "other";
        }

        private static string CreateStableId(string providerId, string countryCode, string location)
        {
            string source = providerId + "|" + countryCode + "|" + location;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
                string suffix = BitConverter.ToString(hash, 0, 6).Replace("-", string.Empty).ToLowerInvariant();
                return providerId + "." + countryCode + "." + suffix;
            }
        }
    }

    public sealed class ServerRowTag
    {
        public ServerRowTag(string serverId, string host)
        {
            ServerId = serverId;
            Host = host;
        }

        public string ServerId { get; private set; }
        public string Host { get; private set; }
    }

    public sealed class ServerMetrics
    {
        public const int WindowSize = 20;
        public const int HistoryLimit = 180;
        private readonly Queue<MetricSample> window = new Queue<MetricSample>();
        private readonly Queue<MetricSample> history = new Queue<MetricSample>();

        public bool HasResult { get; private set; }
        public bool IsOnline { get; private set; }
        public long PingMilliseconds { get; private set; }
        public double JitterMilliseconds { get; private set; }
        public double LossPercent { get; private set; }
        public bool IsMeasurementError { get; private set; }
        public string LastError { get; private set; }
        public DateTime? LastMeasuredUtc { get; private set; }
        public int SampleCount { get { return window.Count; } }
        public int SuccessfulSampleCount { get { return window.Count(x => x.RoundtripTime.HasValue); } }
        public double AverageMilliseconds { get; private set; }
        public long MinimumMilliseconds { get; private set; }
        public long MaximumMilliseconds { get; private set; }
        public MetricSample[] GetHistory() { return history.ToArray(); }
        public DateTime? SessionStartedUtc { get; private set; }
        public long SessionReplies { get; private set; }
        public long SessionFailures { get; private set; }
        public long SessionErrors { get; private set; }
        public long SessionMinimum { get; private set; }
        public long SessionMaximum { get; private set; }
        public long UnresponsiveEpisodes { get; private set; }
        public int ConsecutiveFailures { get; private set; }
        private bool unresponsiveEpisode;

        public void RecordSuccess(long roundtripTime)
        {
            PingMilliseconds = roundtripTime;
            Record(new MetricSample { RoundtripTime = roundtripTime, TimestampUtc = DateTime.UtcNow });
        }

        public void RecordFailure(string error = "TimedOut")
        {
            Record(new MetricSample { Error = error, TimestampUtc = DateTime.UtcNow });
        }

        public void RecordError(string error)
        {
            Record(new MetricSample { Error = error, IsMeasurementError = true, TimestampUtc = DateTime.UtcNow });
        }

        private void Record(MetricSample sample)
        {
            if (!SessionStartedUtc.HasValue) SessionStartedUtc = sample.TimestampUtc;
            if (sample.RoundtripTime.HasValue)
            {
                long value = sample.RoundtripTime.Value;
                SessionMinimum = SessionReplies == 0 ? value : Math.Min(SessionMinimum, value);
                SessionMaximum = SessionReplies == 0 ? value : Math.Max(SessionMaximum, value);
                SessionReplies++; ConsecutiveFailures = 0; unresponsiveEpisode = false;
            }
            else if (sample.IsMeasurementError) { SessionErrors++; ConsecutiveFailures = 0; }
            else
            {
                SessionFailures++; ConsecutiveFailures = Math.Min(ConsecutiveFailures, int.MaxValue - 1) + 1;
                if (ConsecutiveFailures >= 3 && !unresponsiveEpisode) { UnresponsiveEpisodes++; unresponsiveEpisode = true; }
            }
            HasResult = true;
            IsOnline = sample.RoundtripTime.HasValue;
            IsMeasurementError = sample.IsMeasurementError;
            LastError = sample.Error ?? string.Empty;
            LastMeasuredUtc = sample.TimestampUtc;
            history.Enqueue(sample);
            while (history.Count > HistoryLimit) history.Dequeue();
            // Local/DNS errors are not evidence of a lost ICMP packet.
            if (sample.IsMeasurementError) return;
            window.Enqueue(sample);
            while (window.Count > WindowSize) window.Dequeue();
            MetricSample[] samples = window.ToArray();
            long[] successful = samples.Where(x => x.RoundtripTime.HasValue).Select(x => x.RoundtripTime.Value).ToArray();
            LossPercent = (samples.Length - successful.Length) * 100.0 / samples.Length;
            AverageMilliseconds = successful.Length == 0 ? 0 : successful.Average();
            MinimumMilliseconds = successful.Length == 0 ? 0 : successful.Min();
            MaximumMilliseconds = successful.Length == 0 ? 0 : successful.Max();
            double differences = 0;
            int pairs = 0;
            for (int i = 1; i < samples.Length; i++)
            {
                if (!samples[i - 1].RoundtripTime.HasValue || !samples[i].RoundtripTime.HasValue) continue;
                differences += Math.Abs(samples[i].RoundtripTime.Value - samples[i - 1].RoundtripTime.Value);
                pairs++;
            }
            JitterMilliseconds = pairs == 0 ? 0 : differences / pairs;
        }
    }

    public sealed class MetricSample
    {
        public DateTime TimestampUtc { get; set; }
        public long? RoundtripTime { get; set; }
        public string Error { get; set; }
        public bool IsMeasurementError { get; set; }
    }

    public sealed class PingTarget
    {
        public string ServerId { get; set; }
        public string Host { get; set; }
    }

    public sealed class PingMeasurement
    {
        public string ServerId { get; set; }
        public bool IsSuccess { get; set; }
        public long RoundtripTime { get; set; }
        public string Error { get; set; }
        public bool IsMeasurementError { get; set; }
    }
}
