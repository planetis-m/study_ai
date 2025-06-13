using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Validation;

namespace PdfTextAnalyzer.Services;

public class TextCleaningService : AiServiceBase, ITextCleaningService
{
    private readonly PreprocessingSettings _settings;

    public TextCleaningService(
        IAiServiceFactory aiServiceFactory,
        IOptions<PreprocessingSettings> settings)
        : base(aiServiceFactory)
    {
        _settings = Guard.NotNullOptions(settings, nameof(settings));
    }

    public async Task<string> CleanAndFormatTextAsync(string rawText, CancellationToken cancellationToken)
    {
        Guard.NotNullOrWhiteSpace(rawText, nameof(rawText));

        _settings.UserMessage = $"{_settings.TaskPrompt}\n\n---\n\nRaw text:\n{rawText}";

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
            throw new InvalidOperationException($"Failed to clean and format text: {ex.Message}", ex);
        }
    }
}
