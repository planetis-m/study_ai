using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public class TextAnalysisService : AiServiceBase, ITextAnalysisService
{
    private readonly AnalysisSettings _analysisSettings;

    public TextAnalysisService(IOptions<AzureAISettings> aiSettings, IOptions<AnalysisSettings> analysisSettings)
        : base(aiSettings)
    {
        _analysisSettings = analysisSettings.Value;
    }

    public async Task<string> AnalyzeTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));

        var userMessage = $"{_analysisSettings.TaskPrompt}\n\n---\n\nSlide content:\n{text}";

        return await CallAiServiceAsync(
            _analysisSettings.SystemMessage,
            userMessage,
            _analysisSettings.Model.ModelName,
            _analysisSettings.Model.Temperature,
            _analysisSettings.Model.MaxTokens
        );
    }
}
