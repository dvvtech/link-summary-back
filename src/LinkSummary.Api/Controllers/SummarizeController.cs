using LinkSummary.Api.BLL.Abstract;
using LinkSummary.Api.BLL.Services;
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

        public SummarizeController(IWebPageTextExtractor webPageTextExtractor, ISummarizeService summarizeService)
        {
            _webPageTextExtractor = webPageTextExtractor;
            _summarizeService = summarizeService;
        }

        [HttpPost("summarize")]
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
    }
}
