namespace LinkSummary.Api.BLL.Abstract
{
    public interface ISummarizeService
    {
        Task<string> SummarizeTextAsync(string text, CancellationToken cancellationToken = default);
    }
}
