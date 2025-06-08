using Xunit;
using Xunit.Abstractions;

namespace PdfTextAnalyzer.Tests;

public class PdfTextExtractionDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public PdfTextExtractionDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Manual test - requires PDF file")]
    public async Task RunDiagnosticTest_Manual()
    {
        // Update these paths for your test
        var sourcePdfPath = @"C:\path\to\your\test.pdf";
        var outputDirectory = @".\test_output";

        await PdfTextExtractionDiagnosticTest.RunDiagnosticTestAsync(sourcePdfPath, outputDirectory);

        _output.WriteLine($"Diagnostic test completed. Check {outputDirectory} for results.");
    }

    [Theory]
    [InlineData("sample1.pdf")]
    [InlineData("sample2.pdf")]
    public async Task RunDiagnosticTest_MultipleFiles(string pdfFileName)
    {
        var sourcePdfPath = Path.Combine("TestData", pdfFileName);
        var outputDirectory = Path.Combine("TestOutput", Path.GetFileNameWithoutExtension(pdfFileName));

        // Skip if test file doesn't exist
        if (!File.Exists(sourcePdfPath))
        {
            _output.WriteLine($"Test file {sourcePdfPath} not found. Skipping test.");
            return;
        }

        await PdfTextExtractionDiagnosticTest.RunDiagnosticTestAsync(sourcePdfPath, outputDirectory);

        Assert.True(Directory.Exists(outputDirectory));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "comparison_report.txt")));
    }
}
