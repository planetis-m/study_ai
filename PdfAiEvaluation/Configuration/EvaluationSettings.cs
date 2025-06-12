namespace PdfAiEvaluator.Configuration;

public class EvaluationSettings
{
    public const string SectionName = "Evaluation";

    public string StorageRootPath { get; set; } = "./evaluation-cache";
    public string ExecutionName { get; set; } = "PromptQualityEvaluation";
    public bool EnableResponseCaching { get; set; } = true;
    public int TimeToLiveHours { get; set; } = 24;
    public string EvaluatorProvider { get; set; } = "azureai";
    public string EvaluatorModel { get; set; } = "gpt-4o";
    public string TargetProvider { get; set; } = "azureai";
    public string TargetModel { get; set; } = "gpt-4o-mini";
}
