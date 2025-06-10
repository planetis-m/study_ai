using PdfTextAnalyzer.Models;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public interface IPdfAnalysisPipelineCore
{
    Task<PipelineResult> AnalyzePdfAsync(string pdfPath, CancellationToken cancellationToken);
    PipelineSettings GetCurrentSettings();
}
