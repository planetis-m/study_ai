namespace PdfAiEvaluator.Configuration;

public class EvaluationsConfiguration
{
    public const string SectionName = "Evaluations";

    public List<EvaluationSettings> Evaluations { get; set; } = new();

    public EvaluationSettings? GetEvaluationByName(string name)
    {
        return Evaluations.FirstOrDefault(e =>
            string.Equals(e.ExecutionName, name, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasEvaluations => Evaluations.Count > 0;
}
