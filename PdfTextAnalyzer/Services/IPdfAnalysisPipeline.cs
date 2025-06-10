namespace PdfTextAnalyzer.Services;

public interface IPdfAnalysisPipeline
{
    Task AnalyzePdfAsync(string pdfPath, CancellationToken cancellationToken);
}
