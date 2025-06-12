namespace PdfTextAnalyzer.Configuration;

public class ApplicationSettings
{
    public AiSettings AI { get; set; } = new();
    public PipelineSettings Pipeline { get; set; } = new();
    public ArchiveSettings Archive { get; set; } = new();
    public PdfExtractionSettings PdfExtraction { get; set; } = new();
    public PreprocessingSettings Preprocessing { get; set; } = new();
    public AnalysisSettings Analysis { get; set; } = new();
}
