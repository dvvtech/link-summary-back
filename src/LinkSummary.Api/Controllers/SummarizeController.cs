using LinkSummary.Api.BLL.Abstract;
using LinkSummary.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace LinkSummary.Api.Controllers
{
    [ApiController]
    [Route("")]
    public class SummarizeController : ControllerBase
    {
        private readonly IAnalyticsTrackingService _analyticsTrackingService;
        private readonly IWebPageTextExtractor _webPageTextExtractor;
        private readonly ISummarizeService _summarizeService;
        private readonly ILogger<SummarizeController> _logger;

        public SummarizeController(
            IAnalyticsTrackingService analyticsTrackingService,
            IWebPageTextExtractor webPageTextExtractor,
            ISummarizeService summarizeService,
            ILogger<SummarizeController> logger)
        {
            _analyticsTrackingService = analyticsTrackingService;
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
                var clientIp = GetRealClientIp(HttpContext);
                var userAgent = Request.Headers["User-Agent"].ToString();

                _ = _analyticsTrackingService.TrackLinkSummaryVisitAsync(request.Url, clientIp, userAgent);

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

        private string GetRealClientIp(HttpContext context)
        {            
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').First().Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return "unknown";
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
