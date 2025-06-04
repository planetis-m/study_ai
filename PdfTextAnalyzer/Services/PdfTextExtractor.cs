using System.Text;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
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

    public async Task<string> ExtractTextAsync(string pdfPath)
    {
        return await Task.Run(() =>
        {
            var textBuilder = new StringBuilder();
            // Store PageNumber, TextBlock, and the Height of the page the block is on
            var allTextBlocks = new List<(int PageNumber, TextBlock Block, double PageHeight)>();

            using var document = PdfDocument.Open(pdfPath);

            foreach (var page in document.GetPages())
            {
                var pageTextBlocks = GetTextBlocks(page);
                // Add each block with its page number and the page's height
                allTextBlocks.AddRange(pageTextBlocks.Select(block => (page.Number, block, page.Height)));
            }

            var excludedBlocks = new HashSet<TextBlock>();
            if (_settings.ExcludeHeaderFooter && allTextBlocks.Any())
            {
                excludedBlocks = IdentifyHeaderFooterBlocks(allTextBlocks, document.NumberOfPages);
            }

            // Iterate through the collected blocks, now including pageHeight if needed (though not directly here)
            foreach (var (pageNum, block, pageHeight) in allTextBlocks)
            {
                if (!excludedBlocks.Contains(block))
                {
                    var normalizedText = block.Text.Normalize(NormalizationForm.FormKC);
                    textBuilder.AppendLine(normalizedText);
                }
            }
            return textBuilder.ToString();
        });
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

    private HashSet<TextBlock> IdentifyHeaderFooterBlocks(List<(int PageNumber, TextBlock Block, double PageHeight)> allTextBlocks, int totalPages)
    {
        var excludedBlocks = new HashSet<TextBlock>();
        if (!_settings.ExcludeHeaderFooter) return excludedBlocks;

        // Positional Heuristics
        foreach (var (pageNum, block, currentPageHeight) in allTextBlocks)
        {
            if (currentPageHeight <= 0) continue; // Skip if page height is invalid (e.g. 0)

            // Defines the Y coordinate that marks the lower boundary of the header area.
            // Example: page height 1000, margin 10%. Header area is Y=900 to Y=1000. headerBoundary = 900.
            double headerBoundary = currentPageHeight * (1.0 - _settings.HeaderMarginPercentage / 100.0);

            // Defines the Y coordinate that marks the upper boundary of the footer area.
            // Example: page height 1000, margin 10%. Footer area is Y=0 to Y=100. footerBoundary = 100.
            double footerBoundary = currentPageHeight * (_settings.FooterMarginPercentage / 100.0);

            // A block is considered a header if its lowest point (Bottom) is above the headerBoundary.
            // This means the entire block is within the top X% margin.
            // PdfPig Y coordinates start from the bottom of the page.
            if (block.BoundingBox.Bottom > headerBoundary)
            {
                excludedBlocks.Add(block);
            }

            // A block is considered a footer if its highest point (Top) is below the footerBoundary.
            // This means the entire block is within the bottom X% margin.
            else if (block.BoundingBox.Top < footerBoundary)
            {
                excludedBlocks.Add(block);
            }
        }
        return excludedBlocks;
    }
}
