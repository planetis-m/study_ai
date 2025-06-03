namespace PdfTextAnalyzer.Services;

public interface ITextAnalysisService
{
    Task<string> AnalyzeTextAsync(string text);
}
