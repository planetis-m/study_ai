namespace PdfTextAnalyzer.Services;

public interface ITextPreprocessorService
{
    Task<string> CleanAndFormatTextAsync(string rawText);
}
