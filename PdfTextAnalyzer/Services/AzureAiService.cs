using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.Configuration;

namespace PdfTextAnalyzer.Services;

public class AzureAiService : IAzureAiService
{
    private readonly ChatCompletionsClient _client;
    private readonly string _modelName;
    private readonly IConfiguration _configuration;

    public AzureAiService(IConfiguration configuration)
    {
        _configuration = configuration;
        var endpoint = configuration["AzureAI:Endpoint"] ??
            throw new InvalidOperationException("AzureAI:Endpoint not configured");
        var apiKey = configuration["AzureAI:ApiKey"] ??
            throw new InvalidOperationException("AzureAI:ApiKey not configured");
        _modelName = configuration["AzureAI:ModelName"] ?? "gpt-4o-mini";

        _client = new ChatCompletionsClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey),
            new AzureAIInferenceClientOptions()
        );
    }

    public async Task<string> AnalyzeTextAsync(string text)
    {
        var defaultPrompt = GetDefaultUserPrompt();
        var defaultSystemMessage = GetDefaultSystemMessage();
        return await AnalyzeTextAsync(text, defaultPrompt, defaultSystemMessage);
    }

    public async Task<string> AnalyzeTextAsync(string text, string userPrompt)
    {
        var defaultSystemMessage = GetDefaultSystemMessage();
        return await AnalyzeTextAsync(text, userPrompt, defaultSystemMessage);
    }

    public async Task<string> AnalyzeTextAsync(string text, string userPrompt, string systemMessage)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        if (string.IsNullOrWhiteSpace(userPrompt))
            throw new ArgumentException("User prompt cannot be null or empty", nameof(userPrompt));
        if (string.IsNullOrWhiteSpace(systemMessage))
            throw new ArgumentException("System message cannot be null or empty", nameof(systemMessage));

        // Structure the content more clearly
        var userMessageContent = new List<ChatMessageContentItem>
        {
            new ChatMessageTextContentItem("Task: " + userPrompt),
            new ChatMessageTextContentItem("---"),
            new ChatMessageTextContentItem("Document to analyze:"),
            new ChatMessageTextContentItem(text)
        };

        var messages = new List<ChatRequestMessage>
        {
            new ChatRequestSystemMessage(systemMessage),
            new ChatRequestUserMessage(userMessageContent)
        };

        var options = new ChatCompletionsOptions(messages)
        {
            // Temperature = 1.0f,
            // NucleusSamplingFactor = 1.0f,
            MaxTokens = 1000,
            Model = _modelName
        };

        try
        {
            var response = await _client.CompleteAsync(options);
            // Check if response has value before accessing it
            if (!response.HasValue)
            {
                throw new InvalidOperationException("No response received from Azure AI service");
            }
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get response from Azure AI: {ex.Message}", ex);
        }
    }

    private string GetDefaultSystemMessage()
    {
        return _configuration["Analysis:SystemMessage"] ??
               "You are a helpful AI assistant that analyzes and summarizes text content.";
    }

    private string GetDefaultUserPrompt()
    {
        return _configuration["Analysis:DefaultPrompt"] ??
               "Please provide a concise summary of the following document and identify the key points:";
    }
}
