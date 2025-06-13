namespace PdfAiEvaluator.Configuration;

public class EvaluationsConfiguration
{
    public const string SectionName = "Evaluations";

    public EvaluationSettings? GetEvaluationByName(string name)
    {
        return this.FirstOrDefault(e =>
            string.Equals(e.ExecutionName, name, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasEvaluations => this.Count > 0;
}
