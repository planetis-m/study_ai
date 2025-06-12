namespace PdfTextAnalyzer.Configuration;

public class ApplicationSettings
{
    public PipelineSettings Pipeline { get; set; } = new();
    public PdfExtractionSettings PdfExtraction { get; set; } = new();
    public PreprocessingSettings Preprocessing { get; set; } = new();
    public AnalysisSettings Analysis { get; set; } = new();
}
