using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace PdfTextAnalyzer.Services;

public class PdfTextExtractor : IPdfTextExtractor
{
    public async Task<string> ExtractTextAsync(string pdfPath, bool useAdvancedExtraction = false)
    {
        return await Task.Run(() =>
        {
            var textBuilder = new StringBuilder();

            using var document = PdfDocument.Open(pdfPath);

            foreach (var page in document.GetPages())
            {
                // textBuilder.AppendLine($"--- Page {page.Number} ---");
                var pageText = ExtractPageText(page, useAdvancedExtraction);
                textBuilder.Append(pageText);
                // textBuilder.AppendLine();
            }
            return textBuilder.ToString();
        });
    }

    private string ExtractPageText(Page page, bool useAdvancedExtraction)
    {
        if (useAdvancedExtraction)
        {
            return ExtractPageTextAdvanced(page);
        }
        else
        {
            return ExtractPageTextSimple(page);
        }
    }

    private string ExtractPageTextAdvanced(Page page)
    {
        var textBuilder = new StringBuilder();

        // 0. Preprocessing - get letters from the page
        var letters = page.Letters;

        // 1. Extract words using advanced word extractor
        var wordExtractor = NearestNeighbourWordExtractor.Instance;
        var words = wordExtractor.GetWords(letters);

        // 2. Segment page into text blocks with balanced settings
        var pageSegmenterOptions = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions()
        {
            WithinLineBinSize = 15,
            BetweenLineBinSize = 15
        };

        var pageSegmenter = new DocstrumBoundingBoxes(pageSegmenterOptions);
        var textBlocks = pageSegmenter.GetBlocks(words);

        // 3. Detect reading order for better text flow
        var readingOrder = UnsupervisedReadingOrderDetector.Instance;
        var orderedTextBlocks = readingOrder.Get(textBlocks);

        // 4. Extract and normalize text from ordered blocks
        foreach (var block in orderedTextBlocks)
        {
            var normalizedText = block.Text.Normalize(NormalizationForm.FormKC);
            textBuilder.AppendLine(normalizedText);
        }

        return textBuilder.ToString();
    }

    private string ExtractPageTextSimple(Page page)
    {
        var textBuilder = new StringBuilder();

        // 0. Preprocessing - get letters from the page
        var letters = page.Letters;

        // 1. Extract words using advanced word extractor
        var wordExtractor = NearestNeighbourWordExtractor.Instance;
        var words = wordExtractor.GetWords(letters);

        // 2. Use simple default page segmenter as fallback
        var pageSegmenter = DefaultPageSegmenter.Instance;
        var textBlocks = pageSegmenter.GetBlocks(words);

        // 3. Extract and normalize text from blocks
        foreach (var block in textBlocks)
        {
            var normalizedText = block.Text.Normalize(NormalizationForm.FormKC);
            textBuilder.AppendLine(normalizedText);
        }

        return textBuilder.ToString();
    }
}
