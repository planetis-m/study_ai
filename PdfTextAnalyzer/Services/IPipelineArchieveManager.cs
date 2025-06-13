using PdfTextAnalyzer.Models;

namespace PdfTextAnalyzer.Services;

public interface IArchiveManager
{
    Task ArchiveResultAsync(PipelineResult result, CancellationToken cancellationToken);
}
