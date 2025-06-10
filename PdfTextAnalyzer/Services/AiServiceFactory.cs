using Azure;
using GenerativeAI.Microsoft;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public interface IAiServiceFactory
{
    IChatClient CreateChatClient(string provider, string modelId);
}

public class AiServiceFactory : IAiServiceFactory
{
    private readonly AiSettings _aiSettings;

    public AiServiceFactory(IOptions<AiSettings> aiSettings)
    {
        _aiSettings = aiSettings.Value ?? throw new ArgumentNullException(nameof(aiSettings));
    }

    public IChatClient CreateChatClient(string provider, string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        return provider.ToLowerInvariant() switch
        {
            "azureai" => CreateAzureAIChatClient(modelId),
            "googlegenerativeai" => CreateGoogleGenerativeAIChatClient(modelId),
            "openai" => CreateOpenAIChatClient(modelId),
            _ => throw new InvalidOperationException($"Unsupported AI provider: {provider}")
        };
    }

    private IChatClient CreateAzureAIChatClient(string modelId)
    {
        var settings = _aiSettings.AzureAI;

        if (string.IsNullOrWhiteSpace(settings.Endpoint))
            throw new InvalidOperationException("AI:AzureAI:Endpoint not configured");
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("AI:AzureAI:ApiKey not configured");

        // Create Azure AI chat client
        return new Azure.AI.Inference.ChatCompletionsClient(new Uri(settings.Endpoint),
            new AzureKeyCredential(settings.ApiKey)).AsIChatClient(modelId);
    }

    private IChatClient CreateGoogleGenerativeAIChatClient(string modelId)
    {
        var settings = _aiSettings.GoogleGenerativeAI;

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("AI:GoogleGenerativeAI:ApiKey not configured");

        // Create Google Generative AI chat client
        return new GenerativeAIChatClient(settings.ApiKey, modelId);
    }

    private IChatClient CreateOpenAIChatClient(string modelId)
    {
        var settings = _aiSettings.OpenAI;

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("AI:OpenAI:ApiKey not configured");

        // Create OpenAI chat client
        return new OpenAI.Chat.ChatClient(modelId, settings.ApiKey).AsIChatClient();
    }
}
