using LinkSummary.Api.Configuration;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace LinkSummary.Tests
{
    public class ProxyTest
    {
        [Fact]
        public async Task Test1()
        {
            var proxyConfig = new ProxyConfig
            { 
                Url = "http://31.59.20.176:6754",
                Login = "rprptymb",
                Password = ""
            };

            var testEndpoints = new[]
            {
                "https://dns.google/resolve?name=google.com&type=A",
                "https://httpbin.org/get",
                //"https://cloudflare-dns.com/dns-query?name=example.com&type=A",
                "http://ip-api.com/json"
            };

            foreach (var endpoint in testEndpoints)
            {
                var result = await CheckEndpointThroughProxy(endpoint, proxyConfig, CancellationToken.None);
            }
        }

        private async Task<(bool IsHealthy, string ErrorMessage)> CheckEndpointThroughProxy(
            string endpoint,
            ProxyConfig proxyConfig,
            CancellationToken cancellationToken)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    Proxy = new WebProxy
                    {
                        Address = new Uri(proxyConfig.Url),
                        BypassProxyOnLocal = false,
                        UseDefaultCredentials = false,
                        Credentials = (!string.IsNullOrEmpty(proxyConfig.Login) &&
                                       !string.IsNullOrEmpty(proxyConfig.Password))
                            ? new NetworkCredential(proxyConfig.Login, proxyConfig.Password)
                            : null
                    },
                    UseProxy = true,
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                using var httpClient = new HttpClient(handler);
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "HealthCheck");

                var response = await httpClient.GetAsync(endpoint, cancellationToken);

                // Любой ответ (даже 404) означает, что прокси работает
                if (response.StatusCode != HttpStatusCode.BadGateway &&
                    response.StatusCode != HttpStatusCode.GatewayTimeout &&
                    response.StatusCode != HttpStatusCode.ProxyAuthenticationRequired)
                {
                    return (true, null);
                }

                return (false, $"Proxy returned {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"HTTP error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "Timeout");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
    }
}
