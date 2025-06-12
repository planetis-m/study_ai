namespace PdfTextAnalyzer.Services;

public interface IPipelinePresenter
{
    Task AnalyzePdfAsync(string pdfPath, CancellationToken cancellationToken);
}
