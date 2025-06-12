using System.Diagnostics;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Models;

namespace PdfTextAnalyzer.Services;

public class PdfAnalysisPipelineCore : IPdfAnalysisPipelineCore
{
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly ITextCleaningService _textCleaning;
    private readonly ITextAnalysisService _textAnalysis;
    private readonly ApplicationSettings _appSettings;

    public PdfAnalysisPipelineCore(
        IPdfTextExtractor pdfExtractor,
        ITextCleaningService textCleaning,
        ITextAnalysisService textAnalysis,
        ApplicationSettings appSettings)
    {
        _pdfExtractor = pdfExtractor ?? throw new ArgumentNullException(nameof(pdfExtractor));
        _textCleaning = textCleaning ?? throw new ArgumentNullException(nameof(textCleaning));
        _textAnalysis = textAnalysis ?? throw new ArgumentNullException(nameof(textAnalysis));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
    }

    public PipelineSettings GetCurrentSettings() => _appSettings.Pipeline;

    public async Task<PipelineResult> AnalyzePdfAsync(string pdfPath, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(pdfPath))
            throw new ArgumentException("PDF path cannot be null or empty", nameof(pdfPath));

        if (!File.Exists(pdfPath))
            throw new FileNotFoundException($"PDF file not found: {pdfPath}");

        // Step 1: Extract text from PDF
        var extractedText = await _pdfExtractor.ExtractTextAsync(pdfPath, cancellationToken);

        // Step 2: Clean and format the extracted text using preprocessing model
        string? cleanedText = null;
        if (_appSettings.Pipeline.Preprocessing)
        {
            cleanedText = await _textCleaning.CleanAndFormatTextAsync(extractedText, cancellationToken);
        }

        // Step 3: Send cleaned text to main LLM for analysis
        string? analysis = null;
        if (_appSettings.Pipeline.Analysis)
        {
            analysis = await _textAnalysis.AnalyzeTextAsync(cleanedText ?? extractedText, cancellationToken);
        }

        stopwatch.Stop();

        var result = new PipelineResult
        {
            PdfPath = pdfPath,
            ExtractedText = extractedText,
            CleanedText = cleanedText,
            Analysis = analysis,
            ProcessingTime = stopwatch.Elapsed
        };

        // Archive the result if archiving is enabled
        if (_appSettings.Archive.EnableArchiving)
        {
            try
            {
                await PipelineArchiveManager.ArchiveResultAsync(
                    result,
                    _appSettings,
                    _appSettings.Archive.BaseArchiveDirectory);
            }
            catch (Exception archiveEx)
            {
                Console.WriteLine($"Warning: Failed to archive pipeline result: {archiveEx.Message}");
                // Don't fail the pipeline if archiving fails
            }
        }

        return result;
    }
}
