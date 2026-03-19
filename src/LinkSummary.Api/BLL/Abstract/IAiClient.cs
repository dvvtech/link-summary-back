namespace LinkSummary.Api.BLL.Abstract
{
    public interface IAiClient
    {
        Task<string> GetTextResponseAsync(string userPrompt, string systemPrompt, CancellationToken cancellationToken = default);
    }
}
