using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.AI;

namespace PdfAiEvaluator.Configuration;

public class EvaluationSettings
{
    public const string SectionName = "Evaluation";

    [Required]
    public string TestDataPath { get; set; } = string.Empty;
    [Required]
    public string StorageRootPath { get; set; } = "./EvaluationCache";
    [Required]
    public string ExecutionName { get; set; } = "PromptQualityEvaluation";
    public bool EnableResponseCaching { get; set; } = true;
    public int TimeToLiveHours { get; set; } = 6;
    public int MaxConcurrentEvaluations { get; set; } = 1;
    public int IterationsPerTestCase { get; set; } = 3;
    public int ModelResponseTimeout { get; set; } = 3;
    public int EvaluationTimeout { get; set; } = 5;
    [Required]
    public string EvaluatorProvider { get; set; } = "azureai";
    [Required]
    public string EvaluatorModel { get; set; } = string.Empty;
    [Required]
    public string TargetProvider { get; set; } = "azureai";
    [Required]
    public string TargetModel { get; set; } = string.Empty;
    [Required]
    public ChatOptions TargetOptions { get; set; } = new();
}
