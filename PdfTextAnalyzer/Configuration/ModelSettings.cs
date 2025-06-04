namespace PdfTextAnalyzer.Configuration;

public class ModelSettings
{
    public string ModelName { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 4000;
    public float Temperature { get; set; } = 0.0f;
}
