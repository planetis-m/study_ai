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
                var text = page.Text;
                textBuilder.AppendLine($"--- Page {page.Number} ---");
                textBuilder.AppendLine(text);
                textBuilder.AppendLine();
            }

            return textBuilder.ToString();
        });
    }
}
