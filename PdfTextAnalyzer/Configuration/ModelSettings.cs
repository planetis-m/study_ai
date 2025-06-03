namespace PdfTextAnalyzer.Configuration;

public class ModelSettings
{
    public string ModelName { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 1000;
    public float Temperature { get; set; } = 0.0f;
}
