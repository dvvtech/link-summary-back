using LinkSummary.Api.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;

namespace LinkSummary.Api.HealthChecks
{
    public class ProxyHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProxyHealthCheck> _logger;

        // Бесплатные тестовые эндпоинты
        private readonly string[] _testEndpoints = new[]
        {
            "https://dns.google/resolve?name=google.com&type=A",
            "https://httpbin.org/get",
            "https://cloudflare-dns.com/dns-query?name=example.com&type=A",
            "http://ip-api.com/json"
        };

        public ProxyHealthCheck(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ProxyHealthCheck> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var proxyConfig = _configuration.GetSection(ProxyConfig.SectionName).Get<ProxyConfig>();

                if (proxyConfig?.Enabled != true)
                {
                    return HealthCheckResult.Degraded("Proxy is disabled in configuration");
                }

                // Проверяем каждый эндпоинт пока не найдем работающий
                foreach (var endpoint in _testEndpoints)
                {
                    var result = await CheckEndpointThroughProxy(endpoint, proxyConfig, cancellationToken);

                    if (result.IsHealthy)
                    {
                        return HealthCheckResult.Healthy(
                            $"Proxy {proxyConfig.Ip}:{proxyConfig.Port} is working. " +
                            $"Successfully reached {endpoint}");
                    }

                    _logger.LogDebug("Failed to reach {Endpoint} through proxy: {Error}",
                        endpoint, result.ErrorMessage);
                }

                // Если все эндпоинты недоступны
                return HealthCheckResult.Unhealthy(
                    $"Proxy {proxyConfig.Ip}:{proxyConfig.Port} is not reachable. " +
                    "Failed to connect to any test endpoint");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Proxy health check failed with unexpected error");
                return HealthCheckResult.Unhealthy($"Proxy health check failed: {ex.Message}", ex);
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
                        Address = new Uri($"http://{proxyConfig.Ip}:{proxyConfig.Port}"),
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
