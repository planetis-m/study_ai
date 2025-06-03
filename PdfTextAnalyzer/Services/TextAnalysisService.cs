using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public class TextAnalysisService : ITextAnalysisService
{
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly ITextPreprocessorService _textPreprocessor;
    private readonly IAzureAiService _azureAiService;
    private readonly PipelineSettings _pipelineSettings;

    public TextAnalysisService(
        IPdfTextExtractor pdfExtractor,
        ITextPreprocessorService textPreprocessor,
        IAzureAiService azureAiService,
        IOptions<PipelineSettings> pipelineSettings)
    {
        _pdfExtractor = pdfExtractor;
        _textPreprocessor = textPreprocessor;
        _azureAiService = azureAiService;
        _pipelineSettings = pipelineSettings.Value;
    }

    public async Task AnalyzePdfAsync(string pdfPath)
    {
        Console.WriteLine($"Processing PDF: {pdfPath}");

        // Step 1: Extract text from PDF
        Console.WriteLine("Extracting text from PDF...");
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

        Console.WriteLine("\n--- Raw Extracted Text Preview ---");
        Console.WriteLine(preview);
        Console.WriteLine("\n--- End Raw Preview ---\n");

        // Step 2: Clean and format the extracted text using preprocessing model
        string cleanedText = extractedText;
        if (_pipelineSettings.Preprocessing)
        {
            Console.WriteLine("Cleaning and formatting text with preprocessing model...");
            cleanedText = await _textPreprocessor.CleanAndFormatTextAsync(extractedText);

            Console.WriteLine($"Cleaned text: {cleanedText.Length} characters.");

            // Show a preview of cleaned text
            var cleanedPreview = cleanedText.Length > 500
                ? cleanedText.Substring(0, 500) + "..."
                : cleanedText;

            Console.WriteLine("\n--- Cleaned Text Preview ---");
            Console.WriteLine(cleanedPreview);
            Console.WriteLine("\n--- End Cleaned Preview ---\n");
        }
        else
        {
            Console.WriteLine("Text preprocessing is disabled. Using raw extracted text.");
        }

        // Step 3: Send cleaned text to main LLM for analysis
        if (_pipelineSettings.Analysis)
        {
            Console.WriteLine("Sending text to main AI model for analysis...");
            var analysis = await _azureAiService.AnalyzeTextAsync(cleanedText);

            Console.WriteLine("\n--- AI Analysis ---");
            Console.WriteLine(analysis);
            Console.WriteLine("\n--- End Analysis ---");
        }
        else
        {
            Console.WriteLine("Text analysis is disabled.");
        }
    }
}
