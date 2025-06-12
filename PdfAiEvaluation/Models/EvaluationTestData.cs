using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace PdfAiEvaluator.Models;

public class EvaluationTestData
{
    public string TestId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = [];
    public string? GroundTruth { get; set; }
    public string? GroundingContext { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = [];
}

public class EvaluationTestSet
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<EvaluationTestData> TestCases { get; set; } = [];
}
