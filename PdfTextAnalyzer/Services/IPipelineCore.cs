using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Models;

namespace PdfTextAnalyzer.Services;

public interface IPipelineCore
{
    Task<PipelineResult> AnalyzePdfAsync(string pdfPath, CancellationToken cancellationToken);
    PipelineSettings GetCurrentSettings();
}
