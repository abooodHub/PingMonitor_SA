using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PingMonitor
{
    public static class SessionReport
    {
        public static string BuildCsv(IEnumerable<ServerDefinition> servers, IDictionary<string, ServerMetrics> metrics)
        {
            var text = new StringBuilder("Host,FirstMeasurementUtc,LastMeasurementUtc,Replies,NoReply,MeasurementErrors,UnresponsiveEpisodes,MinimumMs,MaximumMs,LossPercent\r\n");
            foreach (ServerDefinition server in servers)
            {
                ServerMetrics value;
                if (!metrics.TryGetValue(server.Id, out value) || !value.HasResult) continue;
                long attempts = value.SessionReplies + value.SessionFailures;
                text.AppendLine(string.Join(",", new[] { Cell(server.Host), Cell(value.SessionStartedUtc.Value.ToString("o")), Cell(value.LastMeasuredUtc.Value.ToString("o")),
                    value.SessionReplies.ToString(CultureInfo.InvariantCulture), value.SessionFailures.ToString(CultureInfo.InvariantCulture), value.SessionErrors.ToString(CultureInfo.InvariantCulture),
                    value.UnresponsiveEpisodes.ToString(CultureInfo.InvariantCulture), value.SessionReplies == 0 ? "" : value.SessionMinimum.ToString(CultureInfo.InvariantCulture),
                    value.SessionReplies == 0 ? "" : value.SessionMaximum.ToString(CultureInfo.InvariantCulture), attempts == 0 ? "" : (value.SessionFailures * 100.0 / attempts).ToString("F2", CultureInfo.InvariantCulture) }));
            }
            return text.ToString();
        }
        private static string Cell(string text)
        {
            if (text.Length > 0 && "=+-@\t\r\n".Contains(text[0])) text = "'" + text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }
}
