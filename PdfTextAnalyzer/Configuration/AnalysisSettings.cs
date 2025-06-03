namespace PdfTextAnalyzer.Configuration;

public class AnalysisSettings
{
    public const string SectionName = "Analysis";

    public ModelSettings Model { get; set; } = new();
    public string SystemMessage { get; set; } = string.Empty;
    public string DefaultPrompt { get; set; } = string.Empty;
}
