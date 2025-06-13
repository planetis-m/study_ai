namespace PdfAiEvaluator.Configuration;

public class ChatSettings
{
    public float? Temperature { get; set; }
    public int? MaxOutputTokens { get; set; } = 1000;
    public float? TopP { get; set; }
    public float? FrequencyPenalty { get; set; }
    public float? PresencePenalty { get; set; }
}
