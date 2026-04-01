using LinkSummary.Api.BLL.Abstract;

namespace LinkSummary.Api.BLL.Services
{
    public class SummarizeService : ISummarizeService
    {
        private readonly IAiClient _aiClient;
        private readonly IPromptService _promptService;

        public SummarizeService(
            IAiClient aiClient,
            IPromptService promptService)
        {
            _aiClient = aiClient;
            _promptService = promptService;
        }

        public async Task<string> SummarizeTextAsync(string text, CancellationToken cancellationToken = default)
        {
            var userPrompt = string.Format(_promptService.GetUserPrompt(version: 1), text);
            return await _aiClient.GetTextResponseAsync(
                userPrompt,
                _promptService.GetSystemPrompt(version: 1),
                cancellationToken);
        }
    }
}
