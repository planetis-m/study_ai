namespace PdfTextAnalyzer.Configuration;

public class PreprocessingSettings
{
    public const string SectionName = "Preprocessing";

    public string ModelName { get; set; } = "gpt-4o-mini";
    public int MaxTokens { get; set; } = 2000;
    public float Temperature { get; set; } = 0.1f;

    public string SystemMessage { get; set; } =
        "You are a text cleaning and formatting assistant. Your job is to clean, format, and structure raw text extracted from PDF slides while preserving all important content and meaning.";

    public string CleaningPrompt { get; set; } =
        @"Clean and format the following raw text extracted from PDF slides. Please:

1. Fix OCR errors and typos
2. Remove unnecessary whitespace and line breaks
3. Structure the content with proper paragraphs and sections
4. Preserve all bullet points, lists, and important formatting
5. Maintain the logical flow and hierarchy of information
6. Keep all technical terms, numbers, and important details intact
7. Remove any artifacts from PDF extraction (like page numbers, headers/footers if not relevant)

Return only the cleaned and properly formatted text, without any additional commentary.";
}
