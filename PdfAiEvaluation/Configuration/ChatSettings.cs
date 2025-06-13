namespace PdfAiEvaluator.Configuration;

public class ChatSettings
{
    public float? Temperature { get; set; } = 0.7f;
    public int? MaxOutputTokens { get; set; } = 4000;
    public float? TopP { get; set; } = null;
    public float? FrequencyPenalty { get; set; } = 0.0f;
    public float? PresencePenalty { get; set; } = 0.0f;
}
