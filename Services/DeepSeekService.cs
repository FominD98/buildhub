using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildHub.Services
{
    public class DeepSeekService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string ApiUrl = "https://api.deepseek.com/v1/chat/completions";

        public DeepSeekService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> SendMessageAsync(string message)
        {
            try
            {
                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                        new { role = "user", content = message }
                    },
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ApiUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"Error: {response.StatusCode} - {responseString}";
                }

                using var document = JsonDocument.Parse(responseString);
                var root = document.RootElement;
                var messageContent = root
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return messageContent ?? "No response";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public string GetServiceName()
        {
            return "DeepSeek";
        }
    }
}
