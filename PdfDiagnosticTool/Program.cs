using PdfDiagnosticTool;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: PdfDiagnosticTool <pdf-file-path> [output-directory]");
            Console.WriteLine("Example: dotnet run sample.pdf ./output");
            return;
        }

        var sourcePdfPath = args[0];
        var outputDirectory = args.Length > 1 ? args[1] : "./diagnostic_output";

        if (!File.Exists(sourcePdfPath))
        {
            Console.WriteLine($"PDF file not found: {sourcePdfPath}");
            return;
        }

        Console.WriteLine($"Running diagnostic analysis on: {sourcePdfPath}");
        Console.WriteLine($"Output directory: {outputDirectory}");

        await PdfTextExtractionDiagnosticTest.RunDiagnosticTestAsync(sourcePdfPath, outputDirectory);
    }
}
