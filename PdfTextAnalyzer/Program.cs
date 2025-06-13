using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Services;

namespace PdfTextAnalyzer;

internal static class ExitCode
{
    public const int Success = 0;
    public const int Failure = 1;
}

class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Setup cancellation token for graceful shutdown
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\nCancellation requested. Shutting down gracefully...");
            cts.Cancel();
        };

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        // Build host with simplified configuration
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register all configuration sections through extension method
                services.RegisterConfiguration(configuration);

                // Register AI service factory
                services.AddSingleton<IAiServiceFactory, AiServiceFactory>();

                // Register services
                services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
                services.AddScoped<ITextCleaningService, TextCleaningService>();
                services.AddScoped<ITextAnalysisService, TextAnalysisService>();

                // Register pipeline services
                services.AddScoped<IPipelineCore, PipelineCore>();
                services.AddScoped<IPipelinePresenter, PipelinePresenter>();
                services.AddScoped<IPipelineArchiveManager, PipelineArchiveManager>();
            })
            .Build();

        // Get the service and run
        var pdfAnalysisPipeline = host.Services.GetRequiredService<IPipelinePresenter>();

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: PdfTextAnalyzer <path-to-pdf-file>");
            Console.WriteLine("Example: PdfTextAnalyzer sample.pdf");
            return ExitCode.Failure;
        }

        var pdfPath = args[0];

        try
        {
            await pdfAnalysisPipeline.AnalyzePdfAsync(pdfPath, cts.Token);
            Console.WriteLine("Processing completed successfully!");
            return ExitCode.Success;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation was cancelled by user.");
            return ExitCode.Failure;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return ExitCode.Failure;
        }
    }
}
