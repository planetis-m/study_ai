namespace PdfTextAnalyzer.Models;

public class PipelineResult
{
    public string PdfPath { get; init; } = string.Empty;
    public TimeSpan ProcessingTime { get; init; }
    public string ExtractedText { get; init; } = string.Empty;
    public string? CleanedText { get; init; }
    public string? Analysis { get; init; }
}
