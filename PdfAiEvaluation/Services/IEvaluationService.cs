namespace PdfAiEvaluator.Services;

public interface IEvaluationService
{
    Task RunEvaluationAsync(string testDataPath);
    Task GenerateReportAsync();
}
