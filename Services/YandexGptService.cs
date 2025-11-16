using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace BuildHub.Services
{
    public class YandexGptService : IAiService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private readonly string _apiKey;
        private readonly string _folderId;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private const string ApiUrl = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

        public YandexGptService(string apiKey, string folderId)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

            if (string.IsNullOrWhiteSpace(folderId))
                throw new ArgumentException("Folder ID cannot be null or empty", nameof(folderId));

            _apiKey = apiKey;
            _folderId = folderId;

            // Создаем retry политику с 3 попытками
            _retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && (int)r.StatusCode >= 500)
                .Or<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s. Reason: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                    });
        }

        public async Task<string> SendMessageAsync(string message)
        {
            return await SendMessageAsync(message, CancellationToken.None);
        }

        public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be null or empty", nameof(message));

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

                // Выполняем запрос с retry политикой
                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
                    {
                        Content = content
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Api-Key", _apiKey);
                    request.Headers.Add("x-folder-id", _folderId);

                    return await _httpClient.SendAsync(request, cancellationToken);
                });

                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"YandexGPT API request failed with status {response.StatusCode}: {responseString}");
                }

                return ParseResponse(responseString);
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException("Request was cancelled");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Failed to communicate with YandexGPT API: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error while processing request: {ex.Message}", ex);
            }
        }

        private string ParseResponse(string responseString)
        {
            try
            {
                using var document = JsonDocument.Parse(responseString);
                var root = document.RootElement;

                // Безопасный парсинг с проверками для YandexGPT структуры
                if (!root.TryGetProperty("result", out var result))
                {
                    throw new InvalidOperationException("Response does not contain 'result' property");
                }

                if (!result.TryGetProperty("alternatives", out var alternatives))
                {
                    throw new InvalidOperationException("Result does not contain 'alternatives' property");
                }

                if (alternatives.GetArrayLength() == 0)
                {
                    throw new InvalidOperationException("Alternatives array is empty");
                }

                var firstAlternative = alternatives[0];
                if (!firstAlternative.TryGetProperty("message", out var messageObj))
                {
                    throw new InvalidOperationException("Alternative does not contain 'message' property");
                }

                if (!messageObj.TryGetProperty("text", out var textProp))
                {
                    throw new InvalidOperationException("Message does not contain 'text' property");
                }

                var messageContent = textProp.GetString();
                if (string.IsNullOrWhiteSpace(messageContent))
                {
                    return "No response content";
                }

                return messageContent;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse JSON response: {ex.Message}", ex);
            }
        }

        public string GetServiceName()
        {
            return "YandexGPT";
        }
    }
}
