using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.AI;
using System.ComponentModel.DataAnnotations;

namespace PdfTextAnalyzer.Configuration;

public class ModelSettings
{
    [Required]
    public string Provider { get; set; } = string.Empty;
    [Required]
    public string ModelName { get; set; } = string.Empty;
    [Required]
    public ChatOptions Options { get; set; } = new();
    [Required]
    public string SystemMessage { get; set; } = string.Empty;
    [Required]
    public string TaskPrompt { get; set; } = string.Empty;
}
