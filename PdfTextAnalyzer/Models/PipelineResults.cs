namespace PdfTextAnalyzer.Models;

public class PipelineResult
{
    public string PdfPath { get; init; } = string.Empty;
    public string? ExtractedText { get; init; }
    public string? CleanedText { get; init; }
    public string? Analysis { get; init; }
    public bool PreprocessingEnabled { get; init; }
    public bool AnalysisEnabled { get; init; }
    public TimeSpan ProcessingTime { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
