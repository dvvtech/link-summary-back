using LinkSummary.Api.BLL.Abstract;
using LinkSummary.Api.Models;
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
                _ = TrackVisitLinkSummaryAsync();

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

        private async Task TrackVisitLinkSummaryAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var clientIp = GetRealClientIp(HttpContext);

            // Создаем запрос к analytics
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "http://analytics-api-container:8080/v1/analytics/track-link-summary");

            request.Headers.Add("X-Forwarded-For", clientIp);
            request.Headers.Add("X-Real-IP", clientIp);
            request.Headers.Add("X-Operation-Type", "calc");

            // Прокидываем оригинальный User-Agent
            var userAgent = Request.Headers["User-Agent"].ToString();
            if (!string.IsNullOrEmpty(userAgent))
            {
                request.Headers.Add("User-Agent", userAgent);
            }
            _logger.LogInformation("Send track-link-summary1");
            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Analytics tracking failed: {response.StatusCode}");
                _logger.LogInformation("error Send track-link-summary1");
            }
            _logger.LogInformation("Send track-link-summary1");
        }

        private string GetRealClientIp(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Берем первый IP из цепочки (реальный клиентский)
                return forwardedFor.Split(',').First().Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Если нет заголовков, используем RemoteIpAddress
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        [HttpGet("test2")]
        public string Test2()
        {
            //_logger.LogInformation("call test2");
            return "1477";
        }
    }
}
