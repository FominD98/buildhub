using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildHub.Services
{
    public class YandexGptService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _folderId;
        private const string ApiUrl = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

        public YandexGptService(string apiKey, string folderId)
        {
            _apiKey = apiKey;
            _folderId = folderId;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Api-Key {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("x-folder-id", _folderId);
        }

        public async Task<string> SendMessageAsync(string message)
        {
            try
            {
                var requestBody = new
                {
                    modelUri = $"gpt://{_folderId}/yandexgpt-lite",
                    completionOptions = new
                    {
                        stream = false,
                        temperature = 0.6,
                        maxTokens = 2000
                    },
                    messages = new[]
                    {
                        new { role = "user", text = message }
                    }
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
                    .GetProperty("result")
                    .GetProperty("alternatives")[0]
                    .GetProperty("message")
                    .GetProperty("text")
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
            return "YandexGPT";
        }
    }
}
