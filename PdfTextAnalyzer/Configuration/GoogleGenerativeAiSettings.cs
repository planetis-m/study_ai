namespace PdfTextAnalyzer.Configuration;

public class GoogleGenerativeAISettings
{
    public const string SectionName = "GoogleAI";

    public string Endpoint { get; set; } = string.Empty; // Optional custom endpoint
    public string ApiKey { get; set; } = string.Empty;
}
