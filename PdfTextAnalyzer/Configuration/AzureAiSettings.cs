namespace PdfTextAnalyzer.Configuration;

public class AzureAISettings
{
    public const string SectionName = "AzureAI";

    public string Endpoint { get; set; } = "https://models.github.ai/inference";
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "mistral-ai/mistral-small-2503";
    public int MaxTokens { get; set; } = 1000;
    public float Temperature { get; set; } = 0.0f;
}
