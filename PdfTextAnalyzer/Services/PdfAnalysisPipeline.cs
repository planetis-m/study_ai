using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public class PdfAnalysisPipeline : IPdfAnalysisPipeline
{
    private readonly IPdfAnalysisPipelineEvaluatable _evaluatablePipeline;

    public PdfAnalysisPipeline(IPdfAnalysisPipelineEvaluatable evaluatablePipeline)
    {
        _evaluatablePipeline = evaluatablePipeline ?? throw new ArgumentNullException(nameof(evaluatablePipeline));
    }

    public async Task AnalyzePdfAsync(string pdfPath, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Processing PDF: {pdfPath}");

        var result = await _evaluatablePipeline.AnalyzePdfAsync(pdfPath, cancellationToken);

        if (!result.IsSuccess)
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
            return;
        }

        if (result.ExtractedText != null)
        {
            Console.WriteLine($"Extracted {result.ExtractedText.Length} characters from PDF.");

            // Show a preview of extracted text
            var preview = result.ExtractedText.Length > 500
                ? result.ExtractedText.Substring(0, 500) + "..."
                : result.ExtractedText;

            Console.WriteLine("\n--- Raw Extracted Text Preview ---");
            Console.WriteLine(preview);
            Console.WriteLine("\n--- End Raw Preview ---\n");
        }

        if (result.CleanedText != null)
        {
            Console.WriteLine($"Cleaned text: {result.CleanedText.Length} characters.");

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

        if (result.Analysis != null)
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
