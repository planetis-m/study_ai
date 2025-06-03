namespace PdfTextAnalyzer.Configuration;

public class PipelineSettings
{
    public const string SectionName = "Pipeline";

    public bool Preprocessing { get; set; } = true;
    public bool Analysis { get; set; } = true;
}
