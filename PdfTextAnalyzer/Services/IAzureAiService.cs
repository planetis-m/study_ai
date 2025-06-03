namespace PdfTextAnalyzer.Services;

public interface IAzureAiService
{
    Task<string> AnalyzeTextAsync(string text);
    Task<string> AnalyzeTextAsync(string text, string userPrompt);
    Task<string> AnalyzeTextAsync(string text, string userPrompt, string systemMessage);
}
