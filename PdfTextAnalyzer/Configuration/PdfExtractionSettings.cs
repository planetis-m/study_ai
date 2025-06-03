namespace PdfTextAnalyzer.Configuration;

public class PdfExtractionSettings
{
    public const string SectionName = "PdfExtraction";

    public bool UseAdvancedExtraction { get; set; } = false;

    // Hard-coded sensible defaults - no need to expose these
    internal int WithinLineBinSize => 15;
    internal int BetweenLineBinSize => 15;
    internal bool UseReadingOrderDetection => true;
}
