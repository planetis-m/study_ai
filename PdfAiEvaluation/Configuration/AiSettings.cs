using System.ComponentModel.DataAnnotations;

namespace PdfAiEvaluator.Configuration;

public class AiSettings
{
    public const string SectionName = "AI";

    public AzureAISettings AzureAI { get; set; } = new();
    public GoogleAISettings GoogleAI { get; set; } = new();
    public OpenAISettings OpenAI { get; set; } = new();
}

public class AzureAISettings
{
    [Required]
    [Url]
    public string Endpoint { get; set; } = "https://models.github.ai/inference";
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}

public class GoogleAISettings
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}

public class OpenAISettings
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
