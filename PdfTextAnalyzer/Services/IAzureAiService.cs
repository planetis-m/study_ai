namespace PdfTextAnalyzer.Services;

public interface IAzureAiService
{
    Task<string> AnalyzeTextAsync(string text);
}
