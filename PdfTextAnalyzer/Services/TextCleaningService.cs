using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public class TextCleaningService : AiServiceBase, ITextCleaningService
{
    private readonly PreprocessingSettings _preprocessingSettings;

    public TextCleaningService(
        IOptions<AzureAISettings> aiSettings,
        IOptions<PreprocessingSettings> preprocessingSettings)
        : base(aiSettings)
    {
        _preprocessingSettings = preprocessingSettings.Value;
    }

    public async Task<string> CleanAndFormatTextAsync(string rawText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            throw new ArgumentException("Raw text cannot be null or empty", nameof(rawText));

        var userMessage = $"{_preprocessingSettings.TaskPrompt}\n\n---\n\nRaw text:\n{rawText}";

        return await CallAiServiceAsync(
            _preprocessingSettings.SystemMessage,
            userMessage,
            _preprocessingSettings.Model.ModelName,
            _preprocessingSettings.Model.Temperature,
            _preprocessingSettings.Model.MaxTokens,
            cancellationToken
        );
    }
}
