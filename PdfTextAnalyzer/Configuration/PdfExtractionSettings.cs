namespace PdfTextAnalyzer.Configuration;

public class PdfExtractionSettings
{
    public const string SectionName = "PdfExtraction";

    public bool UseAdvancedExtraction { get; set; } = true;
    public bool ExcludeHeaderFooter { get; set; } = true;
    public bool UseReadingOrderDetection { get; set; } = true;

    // Hard-coded sensible defaults - no need to expose these
    public int WithinLineBinSize { get; set; } = 15;
    public int BetweenLineBinSize { get; set; } = 15;

    // Header/Footer Detection Settings
    public double HeaderMarginPercentage { get; set; } = 8.0; // top
    public double FooterMarginPercentage { get; set; } = 8.0; // bottom
}
