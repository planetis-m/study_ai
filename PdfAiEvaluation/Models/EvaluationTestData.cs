using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace PdfAiEvaluator.Models;

public class EvaluationTestData
{
    public string TestId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = new();
    public string? GroundTruth { get; set; }
    public string? GroundingContext { get; set; }
    public List<string>? Tags { get; set; }
}

public class EvaluationTestSet
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<EvaluationTestData> TestCases { get; set; } = new();
}
