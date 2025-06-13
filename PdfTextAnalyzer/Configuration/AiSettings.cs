namespace PdfTextAnalyzer.Configuration;

public class AiSettings
{
    public const string SectionName = "AI";

    public AzureAISettings AzureAI { get; set; } = new();
    public GoogleAISettings GoogleAI { get; set; } = new();
    public OpenAISettings OpenAI { get; set; } = new();
}

public class AzureAISettings
{
    public string Endpoint { get; set; } = "https://models.github.ai/inference";
    public string ApiKey { get; set; } = string.Empty;
}

public class GoogleAISettings
{
    public string ApiKey { get; set; } = string.Empty;
}

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
}
