using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using PdfAiEvaluator.Utilities;

namespace PdfAiEvaluator.Models;

public enum EvaluatorType
{
    Coherence,
    Completeness,
    Equivalence,
    Fluency,
    Groundedness,
    Relevance
}

public class EvaluationTestData : TagsHelper.IHasTags
{
    public string TestId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ChatMessage>? Messages { get; set; } = new();
    public string? GoldenAnswer { get; set; }
    public string? GroundTruth { get; set; }
    public string? GroundingContext { get; set; }
    public List<string>? Tags { get; set; }
}

public class EvaluationTestSet : TagsHelper.IHasTags
{
    public string? Name { get; set; } // Human-readable; not used
    public string? Description { get; set; } // Human-readable; not used
    public List<EvaluatorType> Evaluators { get; set; } = new();
    public List<ChatMessage>? Messages { get; set; }
    public List<EvaluationTestData> TestCases { get; set; } = new();
    public List<string>? Tags { get; set; }
}
