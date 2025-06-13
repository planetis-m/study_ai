using Microsoft.Extensions.AI;

namespace PdfTextAnalyzer.Configuration;

public class ModelSettings
{
    public string Provider { get; set; } = "AzureAI"; // Default provider
    public string ModelName { get; set; } = string.Empty;
    public ChatOptions Options { get; set; } = new();
}
