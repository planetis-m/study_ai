using Azure;
using GenerativeAI.Microsoft;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public interface IAiServiceFactory
{
    IChatClient CreateChatClient(string provider, string model);
}

public class AiServiceFactory : IAiServiceFactory
{
    private readonly AiSettings _aiSettings;

    public AiServiceFactory(IOptions<AiSettings> aiSettings)
    {
        _aiSettings = aiSettings.Value ?? throw new ArgumentNullException(nameof(aiSettings));
    }

    public IChatClient CreateChatClient(string provider, string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(model));

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
        var settings = _aiSettings.AzureAI;

        if (string.IsNullOrWhiteSpace(settings.Endpoint))
            throw new InvalidOperationException("AI:AzureAI:Endpoint not configured");

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("AI:AzureAI:ApiKey not configured");

        // Create Azure AI chat client
        return new Azure.AI.Inference.ChatCompletionsClient(new Uri(settings.Endpoint),
            new AzureKeyCredential(settings.ApiKey)).AsIChatClient(model);
    }

    private IChatClient CreateGoogleGenerativeAIChatClient(string model)
    {
        var settings = _aiSettings.GoogleAI;

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("AI:GoogleAI:ApiKey not configured");

        // Create Google Generative AI chat client
        return new GenerativeAIChatClient(settings.ApiKey, model);
    }

    private IChatClient CreateOpenAIChatClient(string model)
    {
        var settings = _aiSettings.OpenAI;

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("AI:OpenAI:ApiKey not configured");

        // Create OpenAI chat client
        return new OpenAI.Chat.ChatClient(model, settings.ApiKey).AsIChatClient();
    }
}
