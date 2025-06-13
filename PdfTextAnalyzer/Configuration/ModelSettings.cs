using Microsoft.Extensions.AI;

namespace PdfTextAnalyzer.Configuration;

public class ModelSettings
{
    public string Provider { get; set; } = "azureai"; // Default provider
    public string ModelName { get; set; } = string.Empty;
    public ChatOptions Options { get; set; } = new();

    public string SystemMessage { get; set; } = string.Empty;
    public string TaskPrompt { get; set; } = string.Empty;
}
