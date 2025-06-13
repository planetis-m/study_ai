using Microsoft.Extensions.Logging;

namespace PdfAiEvaluator.Services;

public static class ReportGenerator
{
    public static void GenerateReport(string storagePath, ILogger logger)
    {
        var fullStoragePath = Path.GetFullPath(storagePath);
        GenerateReportInstructions(fullStoragePath, logger);
    }

    public static void GenerateReportTemplate(ILogger logger)
    {
        GenerateReportInstructions(@"<path\to\your\cache\storage>", logger);
    }

    private static void GenerateReportInstructions(string pathToDisplay, ILogger logger)
    {
        var instructions = $"""
        Report generation instructions:
        1. Install the AI evaluation console tool:
            dotnet new tool-manifest
            dotnet tool install Microsoft.Extensions.AI.Evaluation.Console

        2. Generate HTML report:
            dotnet aieval report --path "{pathToDisplay}" --output report.html --open
        """;

        logger.LogInformation(instructions);
    }
}
