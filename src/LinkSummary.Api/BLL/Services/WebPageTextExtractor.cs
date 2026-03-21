using HtmlAgilityPack;
using LinkSummary.Api.BLL.Abstract;

namespace LinkSummary.Api.BLL.Services
{
    public class WebPageTextExtractor : IWebPageTextExtractor
    {
        private readonly HttpClient _httpClient;

        public WebPageTextExtractor(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<string> ExtractTextFromUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            var html = await _httpClient.GetStringAsync(url, cancellationToken);
            
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var nodesToRemove = htmlDoc.DocumentNode.SelectNodes("//script | //style | //nav | //header | //footer | //aside | //form | //input | //button");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove)
                {
                    node.Remove();
                }
            }

            var textNodes = htmlDoc.DocumentNode.SelectNodes("//p | //h1 | //h2 | //h3 | //h4 | //h5 | //h6");
            
            if (textNodes == null || textNodes.Count == 0)
            {
                return string.Empty;
            }

            var textParts = textNodes
                .Select(node => HtmlEntity.DeEntitize(node.InnerText.Trim()))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Select(text => text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " "))
                .Select(text => string.Join(" ", text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));

            var result = string.Join("\n\n", textParts);

            if (result.Length > 21000)
            {
                result = result.Substring(0, 21000);
            }

            return result;
        }
    }
}
