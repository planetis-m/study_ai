using PdfTextAnalyzer.Models;

namespace PdfTextAnalyzer.Services;

public interface IPdfAnalysisPipelineEvaluatable
{
    Task<PipelineResult> AnalyzePdfAsync(string pdfPath, CancellationToken cancellationToken);
}
