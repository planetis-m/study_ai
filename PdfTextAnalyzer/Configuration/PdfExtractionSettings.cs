namespace PdfTextAnalyzer.Configuration;

public class PdfExtractionSettings
{
    public const string SectionName = "PdfExtraction";

    public bool UseAdvancedExtraction { get; set; } = true;
    public bool ExcludeHeaderFooter { get; set; } = true;

    // Hard-coded sensible defaults - no need to expose these
    internal int WithinLineBinSize => 15;
    internal int BetweenLineBinSize => 15;
    internal bool UseReadingOrderDetection => true;

    // Header/Footer Detection Settings
    internal double HeaderMarginPercentage => 10.0; // top 10%
    internal double FooterMarginPercentage => 10.0; // bottom 10%
}
