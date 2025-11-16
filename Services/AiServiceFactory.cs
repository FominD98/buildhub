namespace BuildHub.Services
{
    public enum AiProvider
    {
        ChatGPT,
        DeepSeek,
        YandexGPT
    }

    public class AiServiceFactory
    {
        private readonly ApiConfig _config;

        public AiServiceFactory(ApiConfig config)
        {
            _config = config;
        }

        public IAiService CreateService(AiProvider provider)
        {
            return provider switch
            {
                AiProvider.ChatGPT => new ChatGptService(_config.OpenAiApiKey),
                AiProvider.DeepSeek => new DeepSeekService(_config.DeepSeekApiKey),
                AiProvider.YandexGPT => new YandexGptService(_config.YandexApiKey, _config.YandexFolderId),
                _ => new ChatGptService(_config.OpenAiApiKey)
            };
        }
    }
}
