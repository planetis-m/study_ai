using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public class AzureAiService : BaseAiService, IAzureAiService
{
    private readonly AnalysisSettings _analysisSettings;

    public AzureAiService(IOptions<AzureAISettings> aiSettings, IOptions<AnalysisSettings> analysisSettings)
        : base(aiSettings)
    {
        _analysisSettings = analysisSettings.Value;
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

        var userMessage = $"Task: {userPrompt}\n\n---\n\nSlide content:\n{text}";

        return await CallAiServiceAsync(
            systemMessage,
            userMessage,
            _analysisSettings.Model.ModelName,
            _analysisSettings.Model.Temperature,
            _analysisSettings.Model.MaxTokens
        );
    }
}
