using Azure;
using GenerativeAI.Microsoft;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Validation;

namespace PdfTextAnalyzer.Services;

public interface IAiServiceFactory
{
    IChatClient CreateChatClient(string provider, string model);
}

public class AiServiceFactory : IAiServiceFactory
{
    private readonly AiSettings _settings;

    public AiServiceFactory(IOptions<AiSettings> settings)
    {
        _settings = Guard.NotNullOptions(settings, nameof(settings));
    }

    public IChatClient CreateChatClient(string provider, string model)
    {
        Guard.NotNullOrWhiteSpace(model, nameof(model));

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

        Guard.ConfigurationNotNullOrWhiteSpace(settings.Endpoint, "AI:AzureAI:Endpoint");
        Guard.ConfigurationNotNullOrWhiteSpace(settings.ApiKey, "AI:AzureAI:ApiKey");

        // Create Azure AI chat client
        return new Azure.AI.Inference.ChatCompletionsClient(new Uri(settings.Endpoint),
            new AzureKeyCredential(settings.ApiKey)).AsIChatClient(model);
    }

    private IChatClient CreateGoogleGenerativeAIChatClient(string model)
    {
        var settings = _settings.GoogleAI;

        Guard.ConfigurationNotNullOrWhiteSpace(settings.ApiKey, "AI:GoogleAI:ApiKey");

        // Create Google Generative AI chat client
        return new GenerativeAIChatClient(settings.ApiKey, model);
    }

    private IChatClient CreateOpenAIChatClient(string model)
    {
        var settings = _settings.OpenAI;

        Guard.ConfigurationNotNullOrWhiteSpace(settings.ApiKey, "AI:GoogleAI:ApiKey");

        // Create OpenAI chat client
        return new OpenAI.Chat.ChatClient(model, settings.ApiKey).AsIChatClient();
    }
}
