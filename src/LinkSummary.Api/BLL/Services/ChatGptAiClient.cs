using LinkSummary.Api.BLL.Abstract;
using System.Text;
using System.Text.Json;

namespace LinkSummary.Api.BLL.Services
{
    public class ChatGptAiClient : IAiClient
    {
        private readonly HttpClient _httpClient;
        //private readonly string _apiKey;
        private readonly string _model;
        private readonly string _apiUrl;

        public ChatGptAiClient(HttpClient httpClient,
                               string model = "gpt-4o")
        {
            _httpClient = httpClient;            
            _model = model;
            _apiUrl = "https://api.openai.com/v1/chat/completions";            
        }

        // Метод для текстовых запросов
        public async Task<string> GetTextResponseAsync(
            string userPrompt,
            string systemPrompt,
            CancellationToken cancellationToken = default)
        {
            return await GetResponseInternalAsync(
                messages: new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                cancellationToken
            );
        }

        // Общий внутренний метод для отправки запроса
        private async Task<string> GetResponseInternalAsync(object[] messages, CancellationToken cancellationToken = default)
        {
            var requestBody = new
            {
                model = _model,
                messages,
                temperature = 0.7
            };

            var jsonRequestBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseContent);
            var choices = jsonDoc.RootElement.GetProperty("choices");
            return choices[0].GetProperty("message").GetProperty("content").GetString();
        }
    }
}
