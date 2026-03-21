using LinkSummary.Api.BLL.Abstract;

namespace LinkSummary.Api.BLL.Services
{
    public class SummarizeService : ISummarizeService
    {
        private readonly IAiClient _aiClient;
        
        private const string SystemPrompt = "Ты - профессиональный суммаризатор текста. Твоя задача - анализировать статьи и создавать их краткое содержание. Всегда отвечай на русском языке, независимо от языка исходного текста.";
        
        private const string UserPromptTemplate = "Проанализируй статью ниже и выдели самые важные моменты. Составь краткое содержание (7-9 предложений), которое передает основную суть статьи. Сохрани ключевые факты и выводы. Отвечай на русском языке. Вот текст статьи:\n\n{0}";

        public SummarizeService(IAiClient aiClient)
        {
            _aiClient = aiClient;
        }

        public async Task<string> SummarizeTextAsync(string text, CancellationToken cancellationToken = default)
        {
            var userPrompt = string.Format(UserPromptTemplate, text);
            return await _aiClient.GetTextResponseAsync(userPrompt, SystemPrompt, cancellationToken);
        }
    }
}
