using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public class TextAnalysisService : AiServiceBase, ITextAnalysisService
{
    private readonly AnalysisSettings _analysisSettings;

    public TextAnalysisService(
        IAiServiceFactory aiServiceFactory,
        IOptions<AiSettings> aiSettings,
        IOptions<AnalysisSettings> analysisSettings)
        : base(aiServiceFactory, aiSettings)
    {
        _analysisSettings = analysisSettings.Value;
    }

    public async Task<string> AnalyzeTextAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));

        var userMessage = $"{_analysisSettings.TaskPrompt}\n\n---\n\nSlide content:\n{text}";

        try
        {
            return await CallAiServiceAsync(
                _analysisSettings.SystemMessage,
                userMessage,
                _analysisSettings.Model,
                cancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            // Re-throw exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            // Wrap other exceptions with more context, but keep the original exception as inner exception
            throw new InvalidOperationException($"Failed to analyze text: {ex.Message}", ex);
        }
    }
}
