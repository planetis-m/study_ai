namespace PdfTextAnalyzer.Configuration;

public class PdfExtractionSettings
{
    public const string SectionName = "PdfExtraction";

    public bool UseAdvancedExtraction { get; set; } = true;
    public bool ExcludeHeaderFooter { get; set; } = true;
    public bool UseReadingOrderDetection { get; set; } = true;

    // Hard-coded sensible defaults - no need to expose these
    internal int WithinLineBinSize => 15;
    internal int BetweenLineBinSize => 15;

    // Header/Footer Detection Settings
    internal double HeaderMarginPercentage => 8.0; // top
    internal double FooterMarginPercentage => 8.0; // bottom
}
