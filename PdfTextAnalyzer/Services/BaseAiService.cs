using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public abstract class BaseAiService
{
    protected readonly ChatCompletionsClient _client;
    protected readonly AzureAISettings _aiSettings;

    protected BaseAiService(IOptions<AzureAISettings> aiSettings)
    {
        _aiSettings = aiSettings.Value;

        if (string.IsNullOrWhiteSpace(_aiSettings.Endpoint))
            throw new InvalidOperationException("AzureAI:Endpoint not configured");
        if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
            throw new InvalidOperationException("AzureAI:ApiKey not configured");

        _client = new ChatCompletionsClient(
            new Uri(_aiSettings.Endpoint),
            new AzureKeyCredential(_aiSettings.ApiKey),
            new AzureAIInferenceClientOptions()
        );
    }

    protected async Task<string> CallAiServiceAsync(
        string systemMessage,
        string userMessage,
        string modelName,
        float temperature,
        int maxTokens)
    {
        if (string.IsNullOrWhiteSpace(systemMessage))
            throw new ArgumentException("System message cannot be null or empty", nameof(systemMessage));
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("User message cannot be null or empty", nameof(userMessage));

        var messages = new List<ChatRequestMessage>
        {
            new ChatRequestSystemMessage(systemMessage),
            new ChatRequestUserMessage(userMessage)
        };

        var options = new ChatCompletionsOptions(messages)
        {
            Temperature = temperature,
            MaxTokens = maxTokens,
            Model = modelName
        };

        try
        {
            var response = await _client.CompleteAsync(options);
            if (!response.HasValue)
            {
                throw new InvalidOperationException($"No response received from Azure AI");
            }
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get response from Azure AI: {ex.Message}", ex);
        }
    }
}
