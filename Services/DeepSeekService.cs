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
    public class DeepSeekService : IAiService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private readonly string _apiKey;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private const string ApiUrl = "https://api.deepseek.com/v1/chat/completions";

        public DeepSeekService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

            _apiKey = apiKey;

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
                    model = "deepseek-chat",
                    messages = new[]
                    {
                        new { role = "user", content = message }
                    },
                    temperature = 0.7
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
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                    return await _httpClient.SendAsync(request, cancellationToken);
                });

                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"DeepSeek API request failed with status {response.StatusCode}: {responseString}");
                }

                return ParseResponse(responseString);
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException("Request was cancelled");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Failed to communicate with DeepSeek API: {ex.Message}", ex);
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

                // Безопасный парсинг с проверками
                if (!root.TryGetProperty("choices", out var choices))
                {
                    throw new InvalidOperationException("Response does not contain 'choices' property");
                }

                if (choices.GetArrayLength() == 0)
                {
                    throw new InvalidOperationException("Choices array is empty");
                }

                var firstChoice = choices[0];
                if (!firstChoice.TryGetProperty("message", out var messageObj))
                {
                    throw new InvalidOperationException("Choice does not contain 'message' property");
                }

                if (!messageObj.TryGetProperty("content", out var contentProp))
                {
                    throw new InvalidOperationException("Message does not contain 'content' property");
                }

                var messageContent = contentProp.GetString();
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
            return "DeepSeek";
        }
    }
}
