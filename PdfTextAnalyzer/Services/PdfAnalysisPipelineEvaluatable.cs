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
        var metadata = new Dictionary<string, object>();

        try
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
                throw new ArgumentException("PDF path cannot be null or empty", nameof(pdfPath));

            if (!File.Exists(pdfPath))
                throw new FileNotFoundException($"PDF file not found: {pdfPath}");

            // Step 1: Extract text from PDF
            var extractionStopwatch = Stopwatch.StartNew();
            var extractedText = await _pdfExtractor.ExtractTextAsync(pdfPath, cancellationToken);
            extractionStopwatch.Stop();

            metadata["ExtractionTimeMs"] = extractionStopwatch.ElapsedMilliseconds;
            metadata["ExtractedTextLength"] = extractedText?.Length ?? 0;

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return new PipelineResult
                {
                    PdfPath = pdfPath,
                    ExtractedText = extractedText,
                    PreprocessingEnabled = _pipelineSettings.Preprocessing,
                    AnalysisEnabled = _pipelineSettings.Analysis,
                    ProcessingTime = stopwatch.Elapsed,
                    Metadata = metadata,
                    IsSuccess = false,
                    ErrorMessage = "No text could be extracted from the PDF."
                };
            }

            // Step 2: Clean and format the extracted text using preprocessing model
            string? cleanedText = extractedText;
            if (_pipelineSettings.Preprocessing)
            {
                var cleaningStopwatch = Stopwatch.StartNew();
                cleanedText = await _textCleaning.CleanAndFormatTextAsync(extractedText, cancellationToken);
                cleaningStopwatch.Stop();

                metadata["CleaningTimeMs"] = cleaningStopwatch.ElapsedMilliseconds;
                metadata["CleanedTextLength"] = cleanedText?.Length ?? 0;
            }

            // Step 3: Send cleaned text to main LLM for analysis
            string? analysis = null;
            if (_pipelineSettings.Analysis && !string.IsNullOrWhiteSpace(cleanedText))
            {
                var analysisStopwatch = Stopwatch.StartNew();
                analysis = await _textAnalysis.AnalyzeTextAsync(cleanedText, cancellationToken);
                analysisStopwatch.Stop();

                metadata["AnalysisTimeMs"] = analysisStopwatch.ElapsedMilliseconds;
                metadata["AnalysisLength"] = analysis?.Length ?? 0;
            }

            stopwatch.Stop();

            return new PipelineResult
            {
                PdfPath = pdfPath,
                ExtractedText = extractedText,
                CleanedText = cleanedText,
                Analysis = analysis,
                PreprocessingEnabled = _pipelineSettings.Preprocessing,
                AnalysisEnabled = _pipelineSettings.Analysis,
                ProcessingTime = stopwatch.Elapsed,
                Metadata = metadata,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new PipelineResult
            {
                PdfPath = pdfPath,
                PreprocessingEnabled = _pipelineSettings.Preprocessing,
                AnalysisEnabled = _pipelineSettings.Analysis,
                ProcessingTime = stopwatch.Elapsed,
                Metadata = metadata,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
