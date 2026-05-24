namespace LinkSummary.Api.Models
{
    public class SummarizeRequest
    {
        public string Url { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
    }
}
