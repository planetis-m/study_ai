using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using PdfAiEvaluator.Models;

namespace PdfAiEvaluator.Utilities;

public static class EvaluationFactory
{
    public static List<IEvaluator> CreateEvaluators(List<EvaluatorType> evaluatorTypes)
    {
        var evaluators = new List<IEvaluator>();

        foreach (var evaluatorType in evaluatorTypes)
        {
            IEvaluator evaluator = evaluatorType switch
            {
                EvaluatorType.Coherence => new CoherenceEvaluator(),
                EvaluatorType.Completeness => new CompletenessEvaluator(),
                EvaluatorType.Equivalence => new EquivalenceEvaluator(),
                EvaluatorType.Fluency => new FluencyEvaluator(),
                EvaluatorType.Groundedness => new GroundednessEvaluator(),
                EvaluatorType.Relevance => new RelevanceEvaluator(),
                _ => throw new ArgumentException($"Unknown evaluator type: {evaluatorType}")
            };

            evaluators.Add(evaluator);
        }

        return evaluators;
    }

    public static List<EvaluationContext> CreateAdditionalContextForScenario(EvaluationTestData testCase)
    {
        var context = new List<EvaluationContext>();

        if (!string.IsNullOrEmpty(testCase.GroundTruth))
        {
            // Add context that uses ground truth
            context.Add(new CompletenessEvaluatorContext(testCase.GroundTruth));
        }

        if (!string.IsNullOrEmpty(testCase.GoldenAnswer))
        {
            // Add context that uses reference answer
            context.Add(new EquivalenceEvaluatorContext(testCase.GoldenAnswer));
        }

        if (!string.IsNullOrEmpty(testCase.GroundingContext))
        {
            // Add context for groundedness evaluation
            context.Add(new GroundednessEvaluatorContext(testCase.GroundingContext));
        }

        return context;
    }
}
