using Azure;
using GenerativeAI.Microsoft;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PdfAiEvaluator.Configuration;

namespace PdfAiEvaluator.Services;

public interface IAiServiceFactory
{
    IChatClient CreateChatClient(string provider, string model);
}

public class AiServiceFactory : IAiServiceFactory
{
    private readonly AiSettings _settings;

    public AiServiceFactory(IOptions<AiSettings> settings)
    {
        _settings = settings.Value;
    }

    public IChatClient CreateChatClient(string provider, string model)
    {
        return provider.ToLowerInvariant() switch
        {
            "azureai" => CreateAzureAIChatClient(model),
            "googleai" => CreateGoogleGenerativeAIChatClient(model),
            "openai" => CreateOpenAIChatClient(model),
            _ => throw new NotSupportedException($"Unsupported AI provider: {provider}")
        };
    }

    private IChatClient CreateAzureAIChatClient(string model)
    {
        var settings = _settings.AzureAI;

        // Create Azure AI chat client
        return new Azure.AI.Inference.ChatCompletionsClient(new Uri(settings.Endpoint),
            new AzureKeyCredential(settings.ApiKey)).AsIChatClient(model);
    }

    private IChatClient CreateGoogleGenerativeAIChatClient(string model)
    {
        var settings = _settings.GoogleAI;

        // Create Google Generative AI chat client
        return new GenerativeAIChatClient(settings.ApiKey, model);
    }

    private IChatClient CreateOpenAIChatClient(string model)
    {
        var settings = _settings.OpenAI;

        // Create OpenAI chat client
        return new OpenAI.Chat.ChatClient(model, settings.ApiKey).AsIChatClient();
    }
}
