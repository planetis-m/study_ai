namespace PdfTextAnalyzer.Configuration;

public class PreprocessingSettings
{
    public const string SectionName = "Preprocessing";

    public ModelSettings Model { get; set; } = new();
    public string SystemMessage { get; set; } = string.Empty;
    public string TaskPrompt { get; set; } = string.Empty;
}
