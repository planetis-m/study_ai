using PdfAiEvaluator.Configuration;

namespace PdfAiEvaluator.Services;

public interface IEvaluationService
{
    Task RunEvaluationAsync(EvaluationSettings settings, CancellationToken cancellationToken);
    Task RunAllEvaluationsAsync(IEnumerable<EvaluationSettings> evaluations, CancellationToken cancellationToken);
    Task GenerateReportAsync(string storagePath, CancellationToken cancellationToken);
}
