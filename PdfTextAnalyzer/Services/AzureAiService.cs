using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public class AzureAiService : IAzureAiService
{
    private readonly ChatCompletionsClient _client;
    private readonly AzureAISettings _aiSettings;
    private readonly AnalysisSettings _analysisSettings;

    public AzureAiService(IOptions<AzureAISettings> aiSettings, IOptions<AnalysisSettings> analysisSettings)
    {
        _aiSettings = aiSettings.Value;
        _analysisSettings = analysisSettings.Value;

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

    public async Task<string> AnalyzeTextAsync(string text)
    {
        return await AnalyzeTextAsync(text, _analysisSettings.DefaultPrompt, _analysisSettings.SystemMessage);
    }

    public async Task<string> AnalyzeTextAsync(string text, string userPrompt)
    {
        return await AnalyzeTextAsync(text, userPrompt, _analysisSettings.SystemMessage);
    }

    public async Task<string> AnalyzeTextAsync(string text, string userPrompt, string systemMessage)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        if (string.IsNullOrWhiteSpace(userPrompt))
            throw new ArgumentException("User prompt cannot be null or empty", nameof(userPrompt));
        if (string.IsNullOrWhiteSpace(systemMessage))
            throw new ArgumentException("System message cannot be null or empty", nameof(systemMessage));

        var messages = new List<ChatRequestMessage>
        {
            new ChatRequestSystemMessage(systemMessage),
            new ChatRequestUserMessage($"Task: {userPrompt}\n\n---\n\nSlide content:\n{text}")
        };

        var options = new ChatCompletionsOptions(messages)
        {
            Temperature = _analysisSettings.Model.Temperature,
            MaxTokens = _analysisSettings.Model.MaxTokens,
            Model = _analysisSettings.Model.ModelName
        };

        try
        {
            var response = await _client.CompleteAsync(options);
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
}
