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
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
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
                services.AddScoped<ITextPreprocessorService, TextPreprocessorService>();
                services.AddScoped<IAzureAiService, AzureAiService>();
                services.AddScoped<ITextAnalysisService, TextAnalysisService>();
            })
            .Build();

        // Get the service and run
        var textAnalysisService = host.Services.GetRequiredService<ITextAnalysisService>();

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: PdfTextAnalyzer <path-to-pdf-file>");
            Console.WriteLine("Example: PdfTextAnalyzer sample.pdf");
            return;
        }

        var pdfPath = args[0];

        if (!File.Exists(pdfPath))
        {
            Console.WriteLine($"Error: File '{pdfPath}' not found.");
            return;
        }

        try
        {
            await textAnalysisService.AnalyzePdfAsync(pdfPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
