using PdfTextAnalyzer.Models;

namespace PdfTextAnalyzer.Services;

public interface IPipelineArchiveManager
{
    Task ArchiveResultAsync(PipelineResult result, CancellationToken cancellationToken);
}
