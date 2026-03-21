namespace LinkSummary.Api.BLL.Abstract
{
    public interface IWebPageTextExtractor
    {
        Task<string> ExtractTextFromUrlAsync(string url, CancellationToken cancellationToken = default);
    }
}
