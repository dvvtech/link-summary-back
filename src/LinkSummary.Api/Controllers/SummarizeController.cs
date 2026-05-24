using FluentValidation;
using LinkSummary.Api.AppStart.Extensions;
using LinkSummary.Api.BLL.Abstract;
using LinkSummary.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace LinkSummary.Api.Controllers
{
    [ApiController]
    [Route("")]
    public class SummarizeController : ControllerBase
    {
        private const string CacheKeyPrefix = "chunks_";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

        private readonly IAnalyticsTrackingService _analyticsTrackingService;
        private readonly IWebPageTextExtractor _webPageTextExtractor;
        private readonly ITextChunker _textChunker;
        private readonly ISummarizeService _summarizeService;
        private readonly IValidator<SummarizeRequest> _summarizeRequestValidator;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SummarizeController> _logger;

        public SummarizeController(
            IAnalyticsTrackingService analyticsTrackingService,
            IWebPageTextExtractor webPageTextExtractor,
            ITextChunker textChunker,
            ISummarizeService summarizeService,
            IValidator<SummarizeRequest> summarizeRequestValidator,
            IMemoryCache cache,
            ILogger<SummarizeController> logger)
        {
            _analyticsTrackingService = analyticsTrackingService;
            _webPageTextExtractor = webPageTextExtractor;
            _textChunker = textChunker;
            _summarizeService = summarizeService;
            _summarizeRequestValidator = summarizeRequestValidator;
            _cache = cache;
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

                var cacheKey = CacheKeyPrefix + request.Url;
                if (!_cache.TryGetValue(cacheKey, out List<string>? chunks))
                {
                    var extractedText = await _webPageTextExtractor.ExtractTextFromUrlAsync(request.Url);

                    if (string.IsNullOrWhiteSpace(extractedText) || extractedText.Length < 100)
                    {
                        return BadRequest(new SummarizeResponse
                        {
                            Success = false,
                            ErrorMessage = "Не удалось извлечь текст из статьи. Возможно, статья слишком короткая или недоступна."
                        });
                    }

                    chunks = _textChunker.Chunk(extractedText);
                    _cache.Set(cacheKey, chunks, CacheDuration);
                }

                var page = request.Page < 1 ? 1 : request.Page;
                if (page > chunks.Count)
                    page = chunks.Count;

                var chunkText = chunks[page - 1];
                var summary = await _summarizeService.SummarizeTextAsync(chunkText);

                return Ok(new SummarizeResponse
                {
                    Success = true,
                    Summary = summary,
                    CurrentPage = page,
                    TotalPages = chunks.Count
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
