using FluentValidation;
using LinkSummary.Api.AppStart.Extensions;
using LinkSummary.Api.BLL.Abstract;
using LinkSummary.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LinkSummary.Api.Controllers
{
    [ApiController]
    [Route("")]
    public class SummarizeController : ControllerBase
    {
        private readonly IAnalyticsTrackingService _analyticsTrackingService;
        private readonly IWebPageTextExtractor _webPageTextExtractor;
        private readonly ISummarizeService _summarizeService;
        private readonly IValidator<SummarizeRequest> _summarizeRequestValidator;
        private readonly ILogger<SummarizeController> _logger;

        public SummarizeController(
            IAnalyticsTrackingService analyticsTrackingService,
            IWebPageTextExtractor webPageTextExtractor,
            ISummarizeService summarizeService,
            IValidator<SummarizeRequest> summarizeRequestValidator,
            ILogger<SummarizeController> logger)
        {
            _analyticsTrackingService = analyticsTrackingService;
            _webPageTextExtractor = webPageTextExtractor;
            _summarizeService = summarizeService;
            _summarizeRequestValidator = summarizeRequestValidator;
            _logger = logger;

        }

        [HttpPost("run")]
        [EnableRateLimiting("SummarizeRequests")]
        public async Task<ActionResult<SummarizeResponse>> Summarize([FromBody] SummarizeRequest request)
        {
            var validationResult = await _summarizeRequestValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new SummarizeResponse
                {
                    Success = false,
                    ErrorMessage = validationResult.Errors[0].ErrorMessage
                });
            }

            try
            {
                var clientIp = HttpContext.GetRealClientIp();
                var userAgent = Request.Headers["User-Agent"].ToString();

                _ = _analyticsTrackingService.TrackVisitAsync(request.Url, clientIp, userAgent);

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

        [HttpGet("test2")]
        public string Test2()
        {
            var clientIp = HttpContext.GetRealClientIp();            

            return "1477";
        }
    }
}
