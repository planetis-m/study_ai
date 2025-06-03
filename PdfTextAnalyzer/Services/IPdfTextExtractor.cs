namespace PdfTextAnalyzer.Services;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(string pdfPath, bool useAdvancedExtraction = false);
}
