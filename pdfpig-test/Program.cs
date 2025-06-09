using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Content;

// Set your paths here
var sourcePdfPath = "sample2.pdf";  // Place your PDF file here
var outputPath = "docstrum_grid_search_output.pdf";
var pageNumber = 15;

// Define parameter variations for grid search
var withinLineMultiplierValues = new[] { 4.0, 5.0, 6.0 };
var betweenLineMultiplierValues = new[] { 1.3, 1.4 };
var withinLineBoundsValues = new[]
{
    // new DocstrumBoundingBoxes.AngleBounds(-20, 20),
    new DocstrumBoundingBoxes.AngleBounds(-30, 30),
    // new DocstrumBoundingBoxes.AngleBounds(-45, 45)
};
var betweenLineBoundsValues = new[]
{
    // new DocstrumBoundingBoxes.AngleBounds(30, 150),
    new DocstrumBoundingBoxes.AngleBounds(45, 135),
    // new DocstrumBoundingBoxes.AngleBounds(60, 120)
};
var angularDifferenceBoundsValues = new[]
{
    // new DocstrumBoundingBoxes.AngleBounds(-20, 20),
    new DocstrumBoundingBoxes.AngleBounds(-30, 30),
    // new DocstrumBoundingBoxes.AngleBounds(-45, 45)
};
var withinLineBinSizeValues = new[] { 10, 5, 3 };
var betweenLineBinSizeValues = new[] { 10, 5, 3 };

try
{
    using (var document = PdfDocument.Open(sourcePdfPath))
    {
        var builder = new PdfDocumentBuilder { };
        PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont titleFont = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        var page = document.GetPage(pageNumber);
        var letters = page.Letters;
        var wordExtractor = NearestNeighbourWordExtractor.Instance;
        var words = wordExtractor.GetWords(letters);
        var readingOrder = UnsupervisedReadingOrderDetector.Instance;

        int configurationNumber = 1;

        // Generate all combinations
        foreach (var withinLineMultiplier in withinLineMultiplierValues)
        {
            foreach (var betweenLineMultiplier in betweenLineMultiplierValues)
            {
                foreach (var withinLineBounds in withinLineBoundsValues)
                {
                    foreach (var betweenLineBounds in betweenLineBoundsValues)
                    {
                        foreach (var angularDifferenceBounds in angularDifferenceBoundsValues)
                        {
                            foreach (var withinLineBinSize in withinLineBinSizeValues)
                            {
                                foreach (var betweenLineBinSize in betweenLineBinSizeValues)
                                {
                                    // Create options for this configuration
                                    var options = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions
                                    {
                                        WithinLineMultiplier = withinLineMultiplier,
                                        BetweenLineMultiplier = betweenLineMultiplier,
                                        WithinLineBounds = withinLineBounds,
                                        BetweenLineBounds = betweenLineBounds,
                                        AngularDifferenceBounds = angularDifferenceBounds,
                                        WithinLineBinSize = withinLineBinSize,
                                        BetweenLineBinSize = betweenLineBinSize
                                    };

                                    // Create page segmenter with custom options
                                    var pageSegmenter = new DocstrumBoundingBoxes(options);
                                    var textBlocks = pageSegmenter.GetBlocks(words);
                                    var orderedTextBlocks = readingOrder.Get(textBlocks);

                                    // Add a new page to the output PDF
                                    var pageBuilder = builder.AddPage(document, pageNumber);

                                    // Set colors for visualization
                                    pageBuilder.SetStrokeColor(0, 255, 0); // Green for bounding boxes
                                    pageBuilder.SetTextAndFillColor(255, 0, 0); // Red for text

                                    // Add title with configuration details
                                    var titleText = $"Config {configurationNumber}: WLM={withinLineMultiplier:F1}, BLM={betweenLineMultiplier:F1}";
                                    var subtitleText = $"WLB=({withinLineBounds.Lower:F0},{withinLineBounds.Upper:F0}), " +
                                                     $"BLB=({betweenLineBounds.Lower:F0},{betweenLineBounds.Upper:F0}), " +
                                                     $"ADB=({angularDifferenceBounds.Lower:F0},{angularDifferenceBounds.Upper:F0})";
                                    var binSizeText = $"WLBS={withinLineBinSize}, BLBS={betweenLineBinSize}";

                                    var titleX = page.Width / 2 + 60;
                                    var titleY = page.Height - 30;
                                    pageBuilder.AddText(titleText, 12, new PdfPoint(titleX, titleY), titleFont);
                                    pageBuilder.AddText(subtitleText, 10, new PdfPoint(titleX, titleY - 15), font);
                                    pageBuilder.AddText(binSizeText, 10, new PdfPoint(titleX, titleY - 30), font);
                                    pageBuilder.AddText($"Blocks found: {orderedTextBlocks.Count()}", 10, new PdfPoint(titleX, titleY - 45), font);

                                    // Draw bounding boxes and reading order
                                    foreach (var block in orderedTextBlocks)
                                    {
                                        var bbox = block.BoundingBox;
                                        pageBuilder.DrawRectangle(bbox.BottomLeft, bbox.Width, bbox.Height);

                                        // Add reading order number
                                        var orderText = block.ReadingOrder.ToString();
                                        var orderPosition = new PdfPoint(bbox.Left + 2, bbox.Top - 2);
                                        pageBuilder.AddText(orderText, 8, orderPosition, font);

                                        // Add subtle line visual diagnostics
                                        pageBuilder.SetStrokeColor(200, 200, 200); // Light gray for line diagnostics
                                        int lineNumber = 1;
                                        foreach (var textLine in block.TextLines)
                                        {
                                            var lineBounds = textLine.BoundingBox;
                                            pageBuilder.DrawLine(new PdfPoint(lineBounds.Left, lineBounds.Bottom - 1),
                                                               new PdfPoint(lineBounds.Right, lineBounds.Bottom - 1));

                                            pageBuilder.AddText(lineNumber.ToString(), 6, new PdfPoint(lineBounds.Right + 2, lineBounds.Bottom - 1), font);
                                            lineNumber++;
                                        }
                                        pageBuilder.SetStrokeColor(0, 255, 0); // Reset to green for bounding boxes
                                    }

                                    // Add configuration summary at bottom
                                    var summaryY = 50;
                                    var totalConfigurations = withinLineMultiplierValues.Length * betweenLineMultiplierValues.Length *
                                                            withinLineBoundsValues.Length * betweenLineBoundsValues.Length *
                                                            angularDifferenceBoundsValues.Length * withinLineBinSizeValues.Length *
                                                            betweenLineBinSizeValues.Length;
                                    var summaryText = $"Configuration {configurationNumber}/{totalConfigurations}";
                                    pageBuilder.AddText(summaryText, 8, new PdfPoint(20, summaryY), font);

                                    configurationNumber++;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Write result to a file
        byte[] fileBytes = builder.Build();
        File.WriteAllBytes(outputPath, fileBytes);

        Console.WriteLine($"Grid search completed successfully!");
        Console.WriteLine($"Generated {configurationNumber - 1} configurations");
        Console.WriteLine($"Output saved to: {outputPath}");
        Console.WriteLine("\nParameter variations tested:");
        Console.WriteLine($"- WithinLineMultiplier: {withinLineMultiplierValues.Length} values");
        Console.WriteLine($"- BetweenLineMultiplier: {betweenLineMultiplierValues.Length} values");
        Console.WriteLine($"- WithinLineBounds: {withinLineBoundsValues.Length} values");
        Console.WriteLine($"- BetweenLineBounds: {betweenLineBoundsValues.Length} values");
        Console.WriteLine($"- AngularDifferenceBounds: {angularDifferenceBoundsValues.Length} values");
        Console.WriteLine($"- WithinLineBinSize: {withinLineBinSizeValues.Length} values");
        Console.WriteLine($"- BetweenLineBinSize: {betweenLineBinSizeValues.Length} values");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
