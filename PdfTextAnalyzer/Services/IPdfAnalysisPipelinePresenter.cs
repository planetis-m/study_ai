namespace PdfTextAnalyzer.Services;

public interface IPdfAnalysisPipelinePresenter
{
    Task AnalyzePdfAsync(string pdfPath, CancellationToken cancellationToken);
}
