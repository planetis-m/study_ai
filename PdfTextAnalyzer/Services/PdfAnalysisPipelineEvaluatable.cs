using System.Diagnostics;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Models;

namespace PdfTextAnalyzer.Services;

public class PdfAnalysisPipelineEvaluatable : IPdfAnalysisPipelineEvaluatable
{
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly ITextCleaningService _textCleaning;
    private readonly ITextAnalysisService _textAnalysis;
    private readonly PipelineSettings _pipelineSettings;

    public PdfAnalysisPipelineEvaluatable(
        IPdfTextExtractor pdfExtractor,
        ITextCleaningService textCleaning,
        ITextAnalysisService textAnalysis,
        IOptions<PipelineSettings> pipelineSettings)
    {
        _pdfExtractor = pdfExtractor ?? throw new ArgumentNullException(nameof(pdfExtractor));
        _textCleaning = textCleaning ?? throw new ArgumentNullException(nameof(textCleaning));
        _textAnalysis = textAnalysis ?? throw new ArgumentNullException(nameof(textAnalysis));
        _pipelineSettings = pipelineSettings.Value ?? throw new ArgumentNullException(nameof(pipelineSettings));
    }

    public async Task<PipelineResult> AnalyzePdfAsync(string pdfPath, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
                throw new ArgumentException("PDF path cannot be null or empty", nameof(pdfPath));

            if (!File.Exists(pdfPath))
                throw new FileNotFoundException($"PDF file not found: {pdfPath}");

            // Step 1: Extract text from PDF
            var extractedText = await _pdfExtractor.ExtractTextAsync(pdfPath, cancellationToken);
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return new PipelineResult
                {
                    PdfPath = pdfPath,
                    ExtractedText = extractedText,
                    ProcessingTime = stopwatch.Elapsed,
                    IsSuccess = false,
                    ErrorMessage = "No text could be extracted from the PDF."
                };
            }

            // Step 2: Clean and format the extracted text using preprocessing model
            string? cleanedText = extractedText;
            if (_pipelineSettings.Preprocessing)
            {
                cleanedText = await _textCleaning.CleanAndFormatTextAsync(extractedText, cancellationToken);
            }

            // Step 3: Send cleaned text to main LLM for analysis
            string? analysis = null;
            if (_pipelineSettings.Analysis && !string.IsNullOrWhiteSpace(cleanedText))
            {
                analysis = await _textAnalysis.AnalyzeTextAsync(cleanedText, cancellationToken);
            }

            stopwatch.Stop();

            return new PipelineResult
            {
                PdfPath = pdfPath,
                ExtractedText = extractedText,
                CleanedText = cleanedText,
                Analysis = analysis,
                ProcessingTime = stopwatch.Elapsed,
                IsSuccess = true
            };
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new PipelineResult
            {
                PdfPath = pdfPath,
                ProcessingTime = stopwatch.Elapsed,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
