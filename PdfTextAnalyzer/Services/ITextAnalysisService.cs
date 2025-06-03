namespace PdfTextAnalyzer.Services;

public interface ITextAnalysisService
{
    Task AnalyzePdfAsync(string pdfPath);
}
