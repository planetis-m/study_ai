using System.Diagnostics;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Models;
using PdfTextAnalyzer.Validation;

namespace PdfTextAnalyzer.Services;

public class PipelineCore : IPipelineCore
{
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly ITextCleaningService _textCleaning;
    private readonly ITextAnalysisService _textAnalysis;
    private readonly IArchiveManager _archiveManager;
    private readonly PipelineSettings _pipelineSettings;

    public PipelineCore(
        IPdfTextExtractor pdfExtractor,
        ITextCleaningService textCleaning,
        ITextAnalysisService textAnalysis,
        IArchiveManager archiveManager,
        IOptions<PipelineSettings> pipelineSettings)
    {
        _pdfExtractor = Guard.NotNull(pdfExtractor, nameof(pdfExtractor));
        _textCleaning = Guard.NotNull(textCleaning, nameof(textCleaning));
        _textAnalysis = Guard.NotNull(textAnalysis, nameof(textAnalysis));
        _archiveManager = Guard.NotNull(archiveManager, nameof(archiveManager));
        _pipelineSettings = Guard.NotNullOptions(pipelineSettings, nameof(pipelineSettings));
    }

    public PipelineSettings GetCurrentSettings() => _pipelineSettings;

    public async Task<PipelineResult> AnalyzePdfAsync(string pdfPath, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        Guard.NotNullOrWhiteSpace(pdfPath, nameof(pdfPath));

        if (!File.Exists(pdfPath))
            throw new FileNotFoundException($"PDF file not found: {pdfPath}");

        // Step 1: Extract text from PDF
        var extractedText = await _pdfExtractor.ExtractTextAsync(pdfPath, cancellationToken);

        // Step 2: Clean and format the extracted text using preprocessing model
        string? cleanedText = null;
        if (_pipelineSettings.Preprocessing)
        {
            cleanedText = await _textCleaning.CleanAndFormatTextAsync(extractedText, cancellationToken);
        }

        // Step 3: Send cleaned text to main LLM for analysis
        string? analysis = null;
        if (_pipelineSettings.Analysis)
        {
            analysis = await _textAnalysis.AnalyzeTextAsync(cleanedText ?? extractedText, cancellationToken);
        }

        stopwatch.Stop();

        var result = new PipelineResult
        {
            PdfPath = pdfPath,
            ProcessingTime = stopwatch.Elapsed,
            ExtractedText = extractedText,
            CleanedText = cleanedText,
            Analysis = analysis
        };

        // Archive the result if archiving is enabled
        if (_pipelineSettings.Archiving)
        {
            await _archiveManager.ArchiveResultAsync(result, cancellationToken);
        }

        return result;
    }
}
