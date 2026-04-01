using LinkSummary.Api.BLL.Abstract;
using LinkSummary.Api.Models;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace LinkSummary.Api.Controllers
{
    [ApiController]
    [Route("")]
    public class SummarizeController : ControllerBase
    {
        private readonly IWebPageTextExtractor _webPageTextExtractor;
        private readonly ISummarizeService _summarizeService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SummarizeController> _logger;

        public SummarizeController(
            IWebPageTextExtractor webPageTextExtractor,
            ISummarizeService summarizeService,
            IHttpClientFactory httpClientFactory,
            ILogger<SummarizeController> logger)
        {
            _webPageTextExtractor = webPageTextExtractor;
            _summarizeService = summarizeService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

        }

        [HttpPost("run")]
        public async Task<ActionResult<SummarizeResponse>> Summarize([FromBody] SummarizeRequest request)
        {            
            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return BadRequest(new SummarizeResponse
                {
                    Success = false,
                    ErrorMessage = "URL не может быть пустым."
                });
            }

            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uriResult) || 
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest(new SummarizeResponse
                {
                    Success = false,
                    ErrorMessage = "Некорректный формат URL."
                });
            }

            try
            {
                _ = TrackVisitLinkSummaryAsync(request.Url);

                var extractedText = await _webPageTextExtractor.ExtractTextFromUrlAsync(request.Url);

                if (string.IsNullOrWhiteSpace(extractedText) || extractedText.Length < 100)
                {
                    return BadRequest(new SummarizeResponse
                    {
                        Success = false,
                        ErrorMessage = "Не удалось извлечь текст из статьи. Возможно, статья слишком короткая или недоступна."
                    });
                }

                var summary = await _summarizeService.SummarizeTextAsync(extractedText);                

                return Ok(new SummarizeResponse
                {
                    Success = true,
                    Summary = summary
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new SummarizeResponse
                {
                    Success = false,
                    ErrorMessage = $"Ошибка при загрузке страницы: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new SummarizeResponse
                {
                    Success = false,
                    ErrorMessage = $"Произошла ошибка: {ex.Message}"
                });
            }
        }

        private async Task TrackVisitLinkSummaryAsync(string link)
        {
            try
            {                
                var httpClient = _httpClientFactory.CreateClient();             
                var clientIp = GetRealClientIp(HttpContext);
                
                // Создаем запрос к analytics
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    "http://analytics_api:8080/v1/analytics/track-link-summary");

                request.Headers.Add("X-Forwarded-For", clientIp);
                request.Headers.Add("X-Real-IP", clientIp);                
                request.Headers.Add("X-Operation-Link", link);
                
                // Прокидываем оригинальный User-Agent
                var userAgent = Request.Headers["User-Agent"].ToString();
                if (!string.IsNullOrEmpty(userAgent))
                {
                    request.Headers.Add("User-Agent", userAgent);
                }
                
                var response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Analytics tracking failed: {response.StatusCode}");
                    _logger.LogInformation("error Send track-link-summary1");
                }                
            }
            catch (Exception ex)
            {
                _logger.LogError("error:" + ex.ToString());
            }
        }

        private string GetRealClientIp(HttpContext context)
        {
            var candidateIps = new List<string>();

            AddHeaderValues(candidateIps, context.Request.Headers["CF-Connecting-IP"].FirstOrDefault());
            AddHeaderValues(candidateIps, context.Request.Headers["True-Client-IP"].FirstOrDefault());
            AddHeaderValues(candidateIps, context.Request.Headers["X-Forwarded-For"].FirstOrDefault());
            AddHeaderValues(candidateIps, context.Request.Headers["X-Real-IP"].FirstOrDefault());

            var remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp != null)
            {
                candidateIps.Add(remoteIp.MapToIPv4().ToString());
            }

            var parsedIps = candidateIps
                .Select(TryParseIp)
                .Where(ip => ip != null)
                .Cast<IPAddress>()
                .ToList();

            var publicIp = parsedIps.FirstOrDefault(ip => !IsPrivateIp(ip));
            if (publicIp != null)
            {
                return publicIp.MapToIPv4().ToString();
            }

            return parsedIps.FirstOrDefault()?.MapToIPv4().ToString() ?? "unknown";
        }

        private static void AddHeaderValues(List<string> candidateIps, string? headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
            {
                return;
            }

            foreach (var ip in headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                candidateIps.Add(ip);
            }
        }

        private static IPAddress? TryParseIp(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return IPAddress.TryParse(value, out var ip) ? ip.MapToIPv4() : null;
        }

        private static bool IsPrivateIp(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip))
            {
                return true;
            }

            var bytes = ip.MapToIPv4().GetAddressBytes();

            return bytes[0] switch
            {
                10 => true,
                127 => true,
                169 when bytes[1] == 254 => true,
                172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
                192 when bytes[1] == 168 => true,
                _ => false
            };
        }

        [HttpGet("test2")]
        public string Test2()
        {
            var clientIp = GetRealClientIp(HttpContext);
            _logger.LogInformation($"ip: {clientIp}");
            return "1477";
        }
    }
}
