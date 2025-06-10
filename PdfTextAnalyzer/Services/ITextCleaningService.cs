namespace PdfTextAnalyzer.Services;

public interface ITextCleaningService
{
    Task<string> CleanAndFormatTextAsync(string rawText, CancellationToken cancellationToken);
}
