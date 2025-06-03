using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PdfTextAnalyzer.Services;

public class PdfTextExtractor : IPdfTextExtractor
{
    public async Task<string> ExtractTextAsync(string pdfPath)
    {
        return await Task.Run(() =>
        {
            using var document = PdfDocument.Open(pdfPath);
            var textBuilder = new System.Text.StringBuilder();

            foreach (Page page in document.GetPages())
            {
                textBuilder.AppendLine($"--- Page {page.Number} ---");

                var words = page.GetWords();
                var pageText = string.Join(" ", words.Select(w => w.Text));
                textBuilder.AppendLine(pageText);
                textBuilder.AppendLine();
            }

            return textBuilder.ToString();
        });
    }
}
