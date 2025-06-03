namespace PdfTextAnalyzer.Configuration;

public class AnalysisSettings
{
    public const string SectionName = "Analysis";

    public string SystemMessage { get; set; } = string.Empty;
    public string DefaultPrompt { get; set; } = string.Empty;
}
