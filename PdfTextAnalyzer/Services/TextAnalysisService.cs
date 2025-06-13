using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Validation;

namespace PdfTextAnalyzer.Services;

public class TextAnalysisService : AiServiceBase, ITextAnalysisService
{
    private readonly AnalysisSettings _settings;

    public TextAnalysisService(
        IAiServiceFactory aiServiceFactory,
        IOptions<AnalysisSettings> settings)
        : base(aiServiceFactory)
    {
        _settings = Guard.NotNullOptions(settings, nameof(settings));
    }

    public async Task<string> AnalyzeTextAsync(string text, CancellationToken cancellationToken)
    {
        Guard.NotNullOrWhiteSpace(text, nameof(text));

        _settings.UserMessage = $"{_settings.TaskPrompt}\n\n---\n\nSlide content:\n{text}";

        try
        {
            return await CallAiServiceAsync(_settings, cancellationToken);
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
