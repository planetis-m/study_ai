namespace PdfTextAnalyzer.Models;

public class PipelineResult
{
    public string PdfPath { get; init; } = string.Empty;
    public string? ExtractedText { get; init; }
    public string? CleanedText { get; init; }
    public string? Analysis { get; init; }
    public TimeSpan ProcessingTime { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
