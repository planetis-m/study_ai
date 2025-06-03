using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public class TextAnalysisService : ITextAnalysisService
{
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly IAzureAiService _azureAiService;
    private readonly AnalysisSettings _settings;

    public TextAnalysisService(
        IPdfTextExtractor pdfExtractor,
        IAzureAiService azureAiService,
        IOptions<AnalysisSettings> settings)
    {
        _pdfExtractor = pdfExtractor;
        _azureAiService = azureAiService;
        _settings = settings.Value;
    }

    public async Task AnalyzePdfAsync(string pdfPath)
    {
        Console.WriteLine($"Processing PDF: {pdfPath}");
        Console.WriteLine("Extracting text from PDF...");

        // Extract text from PDF
        var extractedText = await _pdfExtractor.ExtractTextAsync(pdfPath);

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            Console.WriteLine("No text could be extracted from the PDF.");
            return;
        }

        Console.WriteLine($"Extracted {extractedText.Length} characters from PDF.");

        // Show a preview of extracted text
        var preview = extractedText.Length > 500
            ? extractedText.Substring(0, 500) + "..."
            : extractedText;

        Console.WriteLine("\n--- Extracted Text Preview ---");
        Console.WriteLine(preview);
        Console.WriteLine("\n--- End Preview ---\n");

        // Send to LLM for analysis
        Console.WriteLine("Sending text to Azure AI for analysis...");
        var analysis = await _azureAiService.AnalyzeTextAsync(extractedText);

        Console.WriteLine("\n--- AI Analysis ---");
        Console.WriteLine(analysis);
        Console.WriteLine("\n--- End Analysis ---");
    }
}
