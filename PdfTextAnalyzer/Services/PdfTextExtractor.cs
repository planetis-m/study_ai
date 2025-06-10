using System.Text;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
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

    public async Task<string> ExtractTextAsync(string pdfPath, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var textBuilder = new StringBuilder();

            using var document = PdfDocument.Open(pdfPath);

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pageTextBlocks = GetTextBlocks(page);

                foreach (var block in pageTextBlocks)
                {
                    if (_settings.UseAdvancedExtraction &&
                        _settings.ExcludeHeaderFooter &&
                        IsBlockHeaderOrFooter(block, page.Height))
                    {
                        continue; // Skip header/footer blocks
                    }
                    var normalizedText = block.Text.Normalize(NormalizationForm.FormKC);
                    textBuilder.AppendLine(normalizedText);
                }
                textBuilder.AppendLine("\n---\n");
            }
            return textBuilder.ToString();
        }, cancellationToken);
    }

    private IEnumerable<TextBlock> GetTextBlocks(Page page)
    {
        var letters = page.Letters;
        var wordExtractor = NearestNeighbourWordExtractor.Instance;
        var words = wordExtractor.GetWords(letters);

        IPageSegmenter pageSegmenter;
        if (_settings.UseAdvancedExtraction)
        {
            var pageSegmenterOptions = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions()
            {
                WithinLineMultiplier = _settings.WithinLineMultiplier,
                BetweenLineMultiplier = _settings.BetweenLineMultiplier,
                WithinLineBinSize = _settings.WithinLineBinSize,
                BetweenLineBinSize = _settings.BetweenLineBinSize
            };
            pageSegmenter = new DocstrumBoundingBoxes(pageSegmenterOptions);
        }
        else
        {
            pageSegmenter = DefaultPageSegmenter.Instance;
        }

        var textBlocks = pageSegmenter.GetBlocks(words);

        if (_settings.UseAdvancedExtraction && _settings.UseReadingOrderDetection)
        {
            var readingOrder = UnsupervisedReadingOrderDetector.Instance;
            textBlocks = readingOrder.Get(textBlocks).ToList();
        }
        return textBlocks;
    }

    private bool IsBlockHeaderOrFooter(TextBlock block, double pageHeight)
    {
        // Calculate boundary for the header area (top X% of the page)
        // PdfPig Y coordinates start from the bottom of the page.
        // A block is a header if its lowest point (Bottom) is within the top margin.
        double headerBoundary = pageHeight * (1.0 - _settings.HeaderMarginPercentage / 100.0);
        if (block.BoundingBox.Bottom > headerBoundary)
        {
            return true;
        }

        // Calculate boundary for the footer area (bottom Y% of the page)
        // A block is a footer if its highest point (Top) is within the bottom margin.
        double footerBoundary = pageHeight * (_settings.FooterMarginPercentage / 100.0);
        if (block.BoundingBox.Top < footerBoundary)
        {
            return true;
        }

        return false;
    }
}
