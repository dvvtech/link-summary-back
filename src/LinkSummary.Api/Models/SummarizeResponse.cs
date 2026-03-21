namespace LinkSummary.Api.Models
{
    public class SummarizeResponse
    {
        public bool Success { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
