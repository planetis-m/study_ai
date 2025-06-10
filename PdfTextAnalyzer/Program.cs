using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Services;

namespace PdfTextAnalyzer;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup cancellation token for graceful shutdown
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        // Build host with strongly-typed configuration
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register configuration sections
                services.Configure<AzureAISettings>(configuration.GetSection(AzureAISettings.SectionName));
                services.Configure<PipelineSettings>(configuration.GetSection(PipelineSettings.SectionName));
                services.Configure<PdfExtractionSettings>(configuration.GetSection(PdfExtractionSettings.SectionName));
                services.Configure<AnalysisSettings>(configuration.GetSection(AnalysisSettings.SectionName));
                services.Configure<PreprocessingSettings>(configuration.GetSection(PreprocessingSettings.SectionName));

                // Register services
                services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
                services.AddScoped<ITextCleaningService, TextCleaningService>();
                services.AddScoped<ITextAnalysisService, TextAnalysisService>();

                // Register pipeline services
                services.AddScoped<IPdfAnalysisPipelineEvaluatable, PdfAnalysisPipelineEvaluatable>();
                services.AddScoped<IPdfAnalysisPipeline, PdfAnalysisPipeline>();
            })
            .Build();

        // Get the service and run
        var pdfAnalysisPipeline = host.Services.GetRequiredService<IPdfAnalysisPipeline>();

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: PdfTextAnalyzer <path-to-pdf-file>");
            Console.WriteLine("Example: PdfTextAnalyzer sample.pdf");
            return;
        }

        var pdfPath = args[0];

        try
        {
            await pdfAnalysisPipeline.AnalyzePdfAsync(pdfPath, cts.Token);
            Console.WriteLine("\nProcessing completed successfully!");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nOperation was cancelled by user.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
