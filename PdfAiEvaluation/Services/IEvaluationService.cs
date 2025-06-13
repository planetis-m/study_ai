namespace PdfAiEvaluator.Services;

public interface IEvaluationService
{
    Task RunEvaluationAsync(string testDataPath, CancellationToken cancellationToken);
    Task GenerateReportAsync(CancellationToken cancellationToken);
}
