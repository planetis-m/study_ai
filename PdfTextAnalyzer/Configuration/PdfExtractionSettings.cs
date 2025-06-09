namespace PdfTextAnalyzer.Configuration;

public class PdfExtractionSettings
{
    public const string SectionName = "PdfExtraction";

    public bool UseAdvancedExtraction { get; set; } = true;
    public bool ExcludeHeaderFooter { get; set; } = true;
    public bool UseReadingOrderDetection { get; set; } = false;

    // Hard-coded sensible defaults - no need to expose these
    internal double WithinLineMultiplier => 5.0;
    internal double BetweenLineMultiplier => 1.4;
    internal int WithinLineBinSize => 5;
    internal int BetweenLineBinSize => 5;

    // Header/Footer Detection Settings
    internal double HeaderMarginPercentage => 8.0; // top
    internal double FooterMarginPercentage => 8.0; // bottom
}
