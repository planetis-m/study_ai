using System.Diagnostics;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Models;

namespace PdfTextAnalyzer.Services;

public class PdfAnalysisPipelineCore : IPdfAnalysisPipelineCore
{
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly ITextCleaningService _textCleaning;
    private readonly ITextAnalysisService _textAnalysis;
    private readonly PipelineSettings _pipelineSettings;

    public PdfAnalysisPipelineCore(
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

    public PipelineSettings GetCurrentSettings() => _pipelineSettings;

    public async Task<PipelineResult> AnalyzePdfAsync(string pdfPath, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(pdfPath))
            throw new ArgumentException("PDF path cannot be null or empty", nameof(pdfPath));

        if (!File.Exists(pdfPath))
            throw new FileNotFoundException($"PDF file not found: {pdfPath}");

        // Step 1: Extract text from PDF
        var extractedText = await _pdfExtractor.ExtractTextAsync(pdfPath, cancellationToken);
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            throw new InvalidOperationException("No text could be extracted from the PDF. The file may be empty, corrupted, or contain only images.");
        }

        // Step 2: Clean and format the extracted text using preprocessing model
        string? cleanedText = null;
        if (_pipelineSettings.Preprocessing)
        {
            cleanedText = await _textCleaning.CleanAndFormatTextAsync(extractedText, cancellationToken);

            if (string.IsNullOrWhiteSpace(cleanedText))
            {
                throw new InvalidOperationException("Text cleaning service returned empty result.");
            }
        }

        // Step 3: Send cleaned text to main LLM for analysis
        string? analysis = null;
        if (_pipelineSettings.Analysis && !string.IsNullOrWhiteSpace(cleanedText))
        {
            analysis = await _textAnalysis.AnalyzeTextAsync(cleanedText ?? extractedText, cancellationToken);

            if (string.IsNullOrWhiteSpace(analysis))
            {
                throw new InvalidOperationException("Text analysis service returned empty result.");
            }
        }

        stopwatch.Stop();

        return new PipelineResult
        {
            PdfPath = pdfPath,
            ExtractedText = extractedText,
            CleanedText = cleanedText,
            Analysis = analysis,
            ProcessingTime = stopwatch.Elapsed
        };
    }
}
