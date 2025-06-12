namespace PdfAiEvaluator.Configuration;

public class EvaluationSettings
{
    public const string SectionName = "Evaluation";

    public string StorageRootPath { get; set; } = "./evaluation-cache";
    public string ExecutionName { get; set; } = "PromptQualityEvaluation";
    public bool EnableResponseCaching { get; set; } = true;
    public int TimeToLiveHours { get; set; } = 6;
    public string EvaluatorProvider { get; set; } = "AzureAI";
    public string EvaluatorModel { get; set; } = string.Empty;
    public string TargetProvider { get; set; } = "AzureAI";
    public string TargetModel { get; set; } = string.Empty;
}
