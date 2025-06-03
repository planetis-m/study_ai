using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public class TextPreprocessorService : ITextPreprocessorService
{
    private readonly ChatCompletionsClient _client;
    private readonly AzureAISettings _aiSettings;
    private readonly PreprocessingSettings _preprocessingSettings;

    public TextPreprocessorService(
        IOptions<AzureAISettings> aiSettings,
        IOptions<PreprocessingSettings> preprocessingSettings)
    {
        _aiSettings = aiSettings.Value;
        _preprocessingSettings = preprocessingSettings.Value;

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

    public async Task<string> CleanAndFormatTextAsync(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            throw new ArgumentException("Raw text cannot be null or empty", nameof(rawText));

        var messages = new List<ChatRequestMessage>
        {
            new ChatRequestSystemMessage(_preprocessingSettings.SystemMessage),
            new ChatRequestUserMessage($"{_preprocessingSettings.CleaningPrompt}\n\n---\n\nRaw text:\n{rawText}")
        };

        var options = new ChatCompletionsOptions(messages)
        {
            Temperature = _preprocessingSettings.Temperature,
            MaxTokens = _preprocessingSettings.MaxTokens,
            Model = _preprocessingSettings.ModelName
        };

        try
        {
            var response = await _client.CompleteAsync(options);
            if (!response.HasValue)
            {
                throw new InvalidOperationException("No response received from preprocessing service");
            }
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to preprocess text: {ex.Message}", ex);
        }
    }
}
