using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace PdfTextAnalyzer.Services;

public class PdfTextExtractor : IPdfTextExtractor
{
    public async Task<string> ExtractTextAsync(string pdfPath)
    {
        return await Task.Run(() =>
        {
            var textBuilder = new StringBuilder();

            using var document = PdfDocument.Open(pdfPath);

            foreach (var page in document.GetPages())
            {
                textBuilder.AppendLine($"--- Page {page.Number} ---");

                var wordExtractor = NearestNeighbourWordExtractor.Instance;
                var words = page.GetWords(wordExtractor);

                var textBlocks = DefaultPageSegmenter.Instance.GetBlocks(words);
                foreach (var block in textBlocks) {
                    textBuilder.AppendLine(block.ToString());
                }
            }

            return textBuilder.ToString();
        });
    }
}
