using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Validation;

namespace PdfTextAnalyzer.Services;

public class PipelinePresenter : IPipelinePresenter
{
    private readonly IPipelineCore _pipelineCore;

    public PipelinePresenter(IPipelineCore pipelineCore)
    {
        _pipelineCore = Guard.NotNull(pipelineCore, nameof(pipelineCore));
    }

    public async Task AnalyzePdfAsync(string pdfPath, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Processing PDF: {pdfPath}");

        var result = await _pipelineCore.AnalyzePdfAsync(pdfPath, cancellationToken);
        var settings = _pipelineCore.GetCurrentSettings();

        // If we reach here, the processing was successful
        Console.WriteLine($"Extracted {result.ExtractedText.Length} characters from PDF.");

        // Show a preview of extracted text
        var preview = result.ExtractedText.Length > 500
            ? result.ExtractedText.Substring(0, 500) + "..."
            : result.ExtractedText;

        Console.WriteLine("\n--- Raw Extracted Text Preview ---");
        Console.WriteLine(preview);
        Console.WriteLine("\n--- End Raw Preview ---\n");

        if (settings.Preprocessing)
        {
            Console.WriteLine($"Cleaned text: {result.CleanedText!.Length} characters.");

            // Show a preview of cleaned text
            var cleanedPreview = result.CleanedText.Length > 500
                ? result.CleanedText.Substring(0, 500) + "..."
                : result.CleanedText;

            Console.WriteLine("\n--- Cleaned Text Preview ---");
            Console.WriteLine(cleanedPreview);
            Console.WriteLine("\n--- End Cleaned Preview ---\n");
        }
        else
        {
            Console.WriteLine("Text preprocessing is disabled. Using raw extracted text.");
        }

        if (settings.Analysis)
        {
            Console.WriteLine("\n--- AI Analysis ---");
            Console.WriteLine(result.Analysis);
            Console.WriteLine("\n--- End Analysis ---");
        }
        else
        {
            Console.WriteLine("Text analysis is disabled.");
        }
    }
}
