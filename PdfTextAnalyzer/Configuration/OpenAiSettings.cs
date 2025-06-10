namespace PdfTextAnalyzer.Configuration;

public class OpenAISettings
{
    public const string SectionName = "OpenAI";

    public string Endpoint { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
}
