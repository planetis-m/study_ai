using System.Text;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public class PdfTextExtractor : IPdfTextExtractor
{
    private readonly PdfExtractionSettings _settings;

    public PdfTextExtractor(IOptions<PdfExtractionSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> ExtractTextAsync(string pdfPath)
    {
        return await Task.Run(() =>
        {
            var textBuilder = new StringBuilder();

            using var document = PdfDocument.Open(pdfPath);

            foreach (var page in document.GetPages())
            {
                var pageText = ExtractPageText(page);
                textBuilder.Append(pageText);
            }
            return textBuilder.ToString();
        });
    }

    private string ExtractPageText(Page page)
    {
        return _settings.UseAdvancedExtraction
            ? ExtractPageTextAdvanced(page)
            : ExtractPageTextSimple(page);
    }

    private string ExtractPageTextSimple(Page page)
    {
        var textBuilder = new StringBuilder();
        var letters = page.Letters;

        // Extract words using advanced word extractor
        var wordExtractor = NearestNeighbourWordExtractor.Instance;
        var words = wordExtractor.GetWords(letters);

        // Use simple default page segmenter as fallback
        var pageSegmenter = DefaultPageSegmenter.Instance;
        var textBlocks = pageSegmenter.GetBlocks(words);

        // Extract and normalize text from blocks
        foreach (var block in textBlocks)
        {
            var normalizedText = block.Text.Normalize(NormalizationForm.FormKC);
            textBuilder.AppendLine(normalizedText);
        }

        return textBuilder.ToString();
    }

    private string ExtractPageTextAdvanced(Page page)
    {
        var textBuilder = new StringBuilder();
        var letters = page.Letters;

        // Extract words using advanced word extractor
        var wordExtractor = NearestNeighbourWordExtractor.Instance;
        var words = wordExtractor.GetWords(letters);

        // Segment page into text blocks with configured settings
        var pageSegmenterOptions = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions()
        {
            WithinLineBinSize = _settings.AdvancedOptions.WithinLineBinSize,
            BetweenLineBinSize = _settings.AdvancedOptions.BetweenLineBinSize
        };

        var pageSegmenter = new DocstrumBoundingBoxes(pageSegmenterOptions);
        var textBlocks = pageSegmenter.GetBlocks(words);

        // Apply reading order detection if enabled
        if (_settings.AdvancedOptions.UseReadingOrderDetection)
        {
            var readingOrder = UnsupervisedReadingOrderDetector.Instance;
            textBlocks = readingOrder.Get(textBlocks).ToList();
        }

        // Extract and normalize text from blocks
        foreach (var block in textBlocks)
        {
            var normalizedText = block.Text.Normalize(NormalizationForm.FormKC);
            textBuilder.AppendLine(normalizedText);
        }

        return textBuilder.ToString();
    }
}
