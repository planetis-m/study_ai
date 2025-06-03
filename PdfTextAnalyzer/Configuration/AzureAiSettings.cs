namespace PdfTextAnalyzer.Configuration;

public class AzureAISettings
{
    public const string SectionName = "AzureAI";

    public string Endpoint { get; set; } = "https://models.github.ai/inference";
    public string ApiKey { get; set; } = string.Empty;
}
