using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PingMonitor
{
    public sealed class WanIpInfo
    {
        public string Ip { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
    }

    public sealed class WanIpService
    {
        private static readonly HttpClient Client = CreateClient();

        public async Task<WanIpInfo> GetAsync(CancellationToken cancellationToken)
        {
            const string endpoint = "https://api.ipify.org?format=json";
            using (HttpResponseMessage response = await Client.GetAsync(endpoint, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                byte[] payload = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                IpifyResponse model;
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(IpifyResponse));
                using (MemoryStream stream = new MemoryStream(payload))
                    model = serializer.ReadObject(stream) as IpifyResponse;

                if (model == null || string.IsNullOrWhiteSpace(model.Ip))
                    throw new InvalidOperationException("The WAN IP service returned an invalid response.");

                return new WanIpInfo
                {
                    Ip = model.Ip.Trim(),
                    Country = string.Empty,
                    CountryCode = string.Empty,
                    Region = string.Empty,
                    City = string.Empty
                };
            }
        }

        private static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PingMonitor.SA/1.0");
            return client;
        }

        [DataContract]
        private sealed class IpifyResponse
        {
            [DataMember(Name = "ip")]
            public string Ip { get; set; }
        }
    }
}
