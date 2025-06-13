using Microsoft.Extensions.AI;

namespace PdfAiEvaluator.Configuration;

public class EvaluationSettings
{
    public const string SectionName = "Evaluation";

    public string TestDataPath { get; set; } = string.Empty;
    public string StorageRootPath { get; set; } = "./EvaluationCache";
    public string ExecutionName { get; set; } = "PromptQualityEvaluation";
    public bool EnableResponseCaching { get; set; } = true;
    public int TimeToLiveHours { get; set; } = 6;
    public int MaxConcurrentEvaluations { get; set; } = 1;
    public int IterationsPerTestCase { get; set; } = 3;
    public int ModelResponseTimeout { get; set; } = 3;
    public int EvaluationTimeout { get; set; } = 5;
    public string EvaluatorProvider { get; set; } = "AzureAI";
    public string EvaluatorModel { get; set; } = string.Empty;
    public string TargetProvider { get; set; } = "AzureAI";
    public string TargetModel { get; set; } = string.Empty;
    public ChatOptions TargetOptions { get; set; } = new();
}
