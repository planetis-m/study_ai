using System.Text;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Services;

namespace PdfDiagnosticTool;

public class PdfTextExtractionDiagnosticTest
{
    public static async Task RunDiagnosticTestAsync(string sourcePdfPath, string outputDirectory)
    {
        // Create output directory if it doesn't exist
        Directory.CreateDirectory(outputDirectory);

        var diagnosticResults = new List<ExtractionDiagnostic>();

        // Test configurations to explore different parameter combinations
        var testConfigurations = new[]
        {
            new TestConfiguration("Basic_DefaultPageSegmenter", new PdfExtractionSettings
            {
                UseAdvancedExtraction = false,
                ExcludeHeaderFooter = false,
                UseReadingOrderDetection = false
            }),

            new TestConfiguration("Advanced_DocstrumDefault", new PdfExtractionSettings
            {
                UseAdvancedExtraction = true,
                ExcludeHeaderFooter = false,
                UseReadingOrderDetection = false
            }),

            new TestConfiguration("Advanced_WithReadingOrder", new PdfExtractionSettings
            {
                UseAdvancedExtraction = true,
                ExcludeHeaderFooter = false,
                UseReadingOrderDetection = true
            }),

            new TestConfiguration("Advanced_WithHeaderFooterExclusion", new PdfExtractionSettings
            {
                UseAdvancedExtraction = true,
                ExcludeHeaderFooter = true,
                UseReadingOrderDetection = true
            }),
        };

        // Process each configuration
        foreach (var config in testConfigurations)
        {
            Console.WriteLine($"\n=== Testing Configuration: {config.Name} ===");

            var diagnostic = await ProcessConfigurationAsync(
                sourcePdfPath,
                config,
                Path.Combine(outputDirectory, $"{config.Name}_visualization.pdf"),
                Path.Combine(outputDirectory, $"{config.Name}_extracted_text.txt"));

            diagnosticResults.Add(diagnostic);

            // Print diagnostic summary
            PrintDiagnosticSummary(diagnostic);
        }

        // Generate comprehensive comparison report
        await GenerateComparisonReportAsync(diagnosticResults, Path.Combine(outputDirectory, "comparison_report.txt"));

        Console.WriteLine($"\nDiagnostic test completed. Results saved to: {outputDirectory}");
    }

    private static async Task<ExtractionDiagnostic> ProcessConfigurationAsync(
        string sourcePdfPath,
        TestConfiguration config,
        string visualizationOutputPath,
        string textOutputPath)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Create extractor with test configuration
        var options = Options.Create(config.Settings);
        var extractor = new PdfTextExtractor(options);

        // Extract text using the configured extractor
        var extractedText = await extractor.ExtractTextAsync(sourcePdfPath);
        await File.WriteAllTextAsync(textOutputPath, extractedText);

        var diagnostic = new ExtractionDiagnostic
        {
            ConfigurationName = config.Name,
            Settings = config.Settings,
            ExtractedText = extractedText,
            ExtractionTimeMs = stopwatch.ElapsedMilliseconds,
            TextLength = extractedText.Length,
            LineCount = extractedText.Split('\n').Length,
            WordCount = extractedText.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length
        };

        // Generate visualization
        await GenerateVisualizationAsync(sourcePdfPath, config.Settings, visualizationOutputPath, diagnostic);

        stopwatch.Stop();
        return diagnostic;
    }

    private static async Task GenerateVisualizationAsync(
        string sourcePdfPath,
        PdfExtractionSettings settings,
        string outputPath,
        ExtractionDiagnostic diagnostic)
    {
        using var document = PdfDocument.Open(sourcePdfPath);
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var boldFont = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        var pageNumber = 1; // Analyze first page for visualization
        var page = document.GetPage(pageNumber);
        var pageBuilder = builder.AddPage(document, pageNumber);

        var letters = page.Letters;
        var wordExtractor = NearestNeighbourWordExtractor.Instance;
        var words = wordExtractor.GetWords(letters);

        IPageSegmenter pageSegmenter;
        if (settings.UseAdvancedExtraction)
        {
            var pageSegmenterOptions = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions()
            {
                WithinLineBinSize = settings.WithinLineBinSize,
                BetweenLineBinSize = settings.BetweenLineBinSize
            };
            pageSegmenter = new DocstrumBoundingBoxes(pageSegmenterOptions);
        }
        else
        {
            pageSegmenter = DefaultPageSegmenter.Instance;
        }

        var textBlocks = pageSegmenter.GetBlocks(words).ToList();

        if (settings.UseAdvancedExtraction && settings.UseReadingOrderDetection)
        {
            var readingOrder = UnsupervisedReadingOrderDetector.Instance;
            textBlocks = readingOrder.Get(textBlocks).ToList();
        }

        diagnostic.BlockCount = textBlocks.Count;
        diagnostic.WordsPerBlock = textBlocks.Count > 0 ? words.Count() / (double)textBlocks.Count : 0;

        // Visualization: Draw bounding boxes with different colors for different types
        var colorIndex = 0;
        var colors = new (byte, byte, byte)[]
        {
            (0, 255, 0),    // Green - normal blocks
            (255, 0, 0),    // Red - header/footer blocks
            (0, 0, 255),    // Blue - reading order
            (255, 165, 0)   // Orange - excluded blocks
        };

        foreach (var block in textBlocks)
        {
            var bbox = block.BoundingBox;
            var isHeaderFooter = settings.ExcludeHeaderFooter &&
                                IsBlockHeaderOrFooter(block, page.Height, settings);

            // Choose color based on block type
            var color = isHeaderFooter ? colors[1] : colors[0]; // Red for header/footer, green for normal
            pageBuilder.SetStrokeColor(color.Item1, color.Item2, color.Item3);
            pageBuilder.DrawRectangle(bbox.BottomLeft, bbox.Width, bbox.Height);

            // Add reading order number if enabled
            if (settings.UseReadingOrderDetection)
            {
                pageBuilder.AddText(
                    block.ReadingOrder.ToString(),
                    8,
                    bbox.TopLeft,
                    font);
            }

            // Add block info
            var blockInfo = $"B{colorIndex++}";
            if (isHeaderFooter) blockInfo += " (H/F)";

            pageBuilder.AddText(
                blockInfo,
                6,
                new UglyToad.PdfPig.Core.PdfPoint(bbox.Left, bbox.Bottom - 10),
                font);
        }

        // Add configuration info at the top of the page
        var configInfo = $"Config: {diagnostic.ConfigurationName} | " +
                        $"Blocks: {diagnostic.BlockCount} | " +
                        $"Advanced: {settings.UseAdvancedExtraction} | " +
                        $"ReadingOrder: {settings.UseReadingOrderDetection} | " +
                        $"ExclHdrFtr: {settings.ExcludeHeaderFooter}";

        pageBuilder.AddText(configInfo, 10, new UglyToad.PdfPig.Core.PdfPoint(50, page.Height - 30), boldFont);

        // Add legend
        var legendY = page.Height - 60;
        pageBuilder.SetStrokeColor(0, 255, 0);
        pageBuilder.DrawRectangle(new UglyToad.PdfPig.Core.PdfPoint(50, legendY), 20, 10);
        pageBuilder.AddText("Normal Blocks", 8, new UglyToad.PdfPig.Core.PdfPoint(80, legendY), font);

        pageBuilder.SetStrokeColor(255, 0, 0);
        pageBuilder.DrawRectangle(new UglyToad.PdfPig.Core.PdfPoint(200, legendY), 20, 10);
        pageBuilder.AddText("Header/Footer", 8, new UglyToad.PdfPig.Core.PdfPoint(230, legendY), font);

        var fileBytes = builder.Build();
        await File.WriteAllBytesAsync(outputPath, fileBytes);
    }

    private static bool IsBlockHeaderOrFooter(TextBlock block, double pageHeight, PdfExtractionSettings settings)
    {
        // Replicate the logic from PdfTextExtractor
        double headerBoundary = pageHeight * (1.0 - settings.HeaderMarginPercentage / 100.0);
        if (block.BoundingBox.Bottom > headerBoundary)
        {
            return true;
        }

        double footerBoundary = pageHeight * (settings.FooterMarginPercentage / 100.0);
        if (block.BoundingBox.Top < footerBoundary)
        {
            return true;
        }

        return false;
    }

    private static void PrintDiagnosticSummary(ExtractionDiagnostic diagnostic)
    {
        Console.WriteLine($"Configuration: {diagnostic.ConfigurationName}");
        Console.WriteLine($"  - UseAdvancedExtraction: {diagnostic.Settings.UseAdvancedExtraction}");
        Console.WriteLine($"  - ExcludeHeaderFooter: {diagnostic.Settings.ExcludeHeaderFooter}");
        Console.WriteLine($"  - UseReadingOrderDetection: {diagnostic.Settings.UseReadingOrderDetection}");
        Console.WriteLine($"  - Extraction Time: {diagnostic.ExtractionTimeMs}ms");
        Console.WriteLine($"  - Text Length: {diagnostic.TextLength} characters");
        Console.WriteLine($"  - Line Count: {diagnostic.LineCount}");
        Console.WriteLine($"  - Word Count: {diagnostic.WordCount}");
        Console.WriteLine($"  - Block Count: {diagnostic.BlockCount}");
        Console.WriteLine($"  - Avg Words per Block: {diagnostic.WordsPerBlock:F2}");
        Console.WriteLine();
    }

    private static async Task GenerateComparisonReportAsync(
        List<ExtractionDiagnostic> diagnostics,
        string reportPath)
    {
        var report = new StringBuilder();
        report.AppendLine("PDF Text Extraction Diagnostic Report");
        report.AppendLine("=====================================");
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        // Summary table
        report.AppendLine("Configuration Summary:");
        report.AppendLine("--------------------");
        report.AppendFormat("{0,-30} {1,-8} {2,-8} {3,-8} {4,-10} {5,-8} {6,-8}\n",
            "Configuration", "Time(ms)", "Chars", "Lines", "Words", "Blocks", "W/Block");
        report.AppendLine(new string('-', 90));

        foreach (var d in diagnostics)
        {
            report.AppendFormat("{0,-30} {1,-8} {2,-8} {3,-8} {4,-10} {5,-8} {6,-8:F1}\n",
                d.ConfigurationName, d.ExtractionTimeMs, d.TextLength,
                d.LineCount, d.WordCount, d.BlockCount, d.WordsPerBlock);
        }

        report.AppendLine();
        report.AppendLine("Configuration Details:");
        report.AppendLine("---------------------");

        foreach (var d in diagnostics)
        {
            report.AppendLine($"{d.ConfigurationName}:");
            report.AppendLine($"  Advanced Extraction: {d.Settings.UseAdvancedExtraction}");
            report.AppendLine($"  Reading Order Detection: {d.Settings.UseReadingOrderDetection}");
            report.AppendLine($"  Header/Footer Exclusion: {d.Settings.ExcludeHeaderFooter}");
            report.AppendLine($"  Within Line Bin Size: {d.Settings.WithinLineBinSize}");
            report.AppendLine($"  Between Line Bin Size: {d.Settings.BetweenLineBinSize}");
            report.AppendLine($"  Header Margin %: {d.Settings.HeaderMarginPercentage}");
            report.AppendLine($"  Footer Margin %: {d.Settings.FooterMarginPercentage}");
            report.AppendLine();
        }

        // Performance analysis
        report.AppendLine("Performance Analysis:");
        report.AppendLine("--------------------");
        var fastestConfig = diagnostics.OrderBy(d => d.ExtractionTimeMs).First();
        var slowestConfig = diagnostics.OrderByDescending(d => d.ExtractionTimeMs).First();

        report.AppendLine($"Fastest Configuration: {fastestConfig.ConfigurationName} ({fastestConfig.ExtractionTimeMs}ms)");
        report.AppendLine($"Slowest Configuration: {slowestConfig.ConfigurationName} ({slowestConfig.ExtractionTimeMs}ms)");
        report.AppendLine($"Performance Difference: {slowestConfig.ExtractionTimeMs - fastestConfig.ExtractionTimeMs}ms");
        report.AppendLine();

        // Text quality analysis
        report.AppendLine("Text Quality Analysis:");
        report.AppendLine("---------------------");
        var mostWords = diagnostics.OrderByDescending(d => d.WordCount).First();
        var fewestWords = diagnostics.OrderBy(d => d.WordCount).First();

        report.AppendLine($"Most Words Extracted: {mostWords.ConfigurationName} ({mostWords.WordCount} words)");
        report.AppendLine($"Fewest Words Extracted: {fewestWords.ConfigurationName} ({fewestWords.WordCount} words)");
        report.AppendLine($"Word Count Difference: {mostWords.WordCount - fewestWords.WordCount} words");

        await File.WriteAllTextAsync(reportPath, report.ToString());
    }
}

public class TestConfiguration
{
    public string Name { get; }
    public PdfExtractionSettings Settings { get; }

    public TestConfiguration(string name, PdfExtractionSettings settings)
    {
        Name = name;
        Settings = settings;
    }
}

public class ExtractionDiagnostic
{
    public string ConfigurationName { get; set; } = string.Empty;
    public PdfExtractionSettings Settings { get; set; } = new();
    public string ExtractedText { get; set; } = string.Empty;
    public long ExtractionTimeMs { get; set; }
    public int TextLength { get; set; }
    public int LineCount { get; set; }
    public int WordCount { get; set; }
    public int BlockCount { get; set; }
    public double WordsPerBlock { get; set; }
}
