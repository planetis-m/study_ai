using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public class TextCleaningService : AiServiceBase, ITextCleaningService
{
    private readonly PreprocessingSettings _preprocessingSettings;

    public TextCleaningService(
        IAiServiceFactory aiServiceFactory,
        IOptions<AiSettings> aiSettings,
        IOptions<PreprocessingSettings> preprocessingSettings)
        : base(aiServiceFactory, aiSettings)
    {
        _preprocessingSettings = preprocessingSettings.Value;
    }

    public async Task<string> CleanAndFormatTextAsync(string rawText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            throw new ArgumentException("Raw text cannot be null or empty", nameof(rawText));

        var userMessage = $"{_preprocessingSettings.TaskPrompt}\n\n---\n\nRaw text:\n{rawText}";

        try
        {
            return await CallAiServiceAsync(
                _preprocessingSettings.SystemMessage,
                userMessage,
                _preprocessingSettings.Model,
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
            throw new InvalidOperationException($"Failed to clean and format text: {ex.Message}", ex);
        }
    }
}
