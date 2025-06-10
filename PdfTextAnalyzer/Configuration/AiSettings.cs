namespace PdfTextAnalyzer.Configuration;

public class AiSettings
{
    public const string SectionName = "AI";

    public AzureAISettings AzureAI { get; set; } = new();
    public GoogleGenerativeAISettings GoogleGenerativeAI { get; set; } = new();
    public OpenAISettings OpenAI { get; set; } = new();
}
