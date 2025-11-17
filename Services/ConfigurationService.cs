using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace BuildHub.Services
{
    public class ConfigurationService
    {
        private static ConfigurationService? _instance;
        private readonly IConfiguration _configuration;

        private ConfigurationService()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        public static ConfigurationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConfigurationService();
                }
                return _instance;
            }
        }

        public string GetOpenAiApiKey() => _configuration["AiServices:OpenAI:ApiKey"] ?? string.Empty;
        public string GetOpenAiModel() => _configuration["AiServices:OpenAI:Model"] ?? "gpt-4o-mini";
        public double GetOpenAiTemperature() => double.Parse(_configuration["AiServices:OpenAI:Temperature"] ?? "0.7");

        public string GetDeepSeekApiKey() => _configuration["AiServices:DeepSeek:ApiKey"] ?? string.Empty;
        public string GetDeepSeekModel() => _configuration["AiServices:DeepSeek:Model"] ?? "deepseek-chat";
        public double GetDeepSeekTemperature() => double.Parse(_configuration["AiServices:DeepSeek:Temperature"] ?? "0.7");

        public string GetYandexApiKey() => _configuration["AiServices:YandexGPT:ApiKey"] ?? string.Empty;
        public string GetYandexFolderId() => _configuration["AiServices:YandexGPT:FolderId"] ?? string.Empty;
        public string GetYandexModel() => _configuration["AiServices:YandexGPT:Model"] ?? "yandexgpt-lite";
        public double GetYandexTemperature() => double.Parse(_configuration["AiServices:YandexGPT:Temperature"] ?? "0.6");
        public int GetYandexMaxTokens() => int.Parse(_configuration["AiServices:YandexGPT:MaxTokens"] ?? "2000");

        public int GetDefaultMaxRetries() => int.Parse(_configuration["Agents:DefaultMaxRetries"] ?? "3");
        public int GetDefaultTimeout() => int.Parse(_configuration["Agents:DefaultTimeout"] ?? "60");

        public ApiConfig GetApiConfig()
        {
            return new ApiConfig
            {
                OpenAiApiKey = GetOpenAiApiKey(),
                DeepSeekApiKey = GetDeepSeekApiKey(),
                YandexApiKey = GetYandexApiKey(),
                YandexFolderId = GetYandexFolderId()
            };
        }
    }
}
