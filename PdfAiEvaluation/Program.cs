using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfAiEvaluator.Configuration;
using PdfAiEvaluator.Services;

namespace PdfAiEvaluator;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        // Build host
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));
                services.Configure<EvaluationSettings>(configuration.GetSection(EvaluationSettings.SectionName));

                // Services
                services.AddSingleton<IAiServiceFactory, AiServiceFactory>();
                services.AddScoped<IEvaluationService, EvaluationService>();

                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var evaluationService = host.Services.GetRequiredService<IEvaluationService>();
        var settings = host.Services.GetRequiredService<IOptions<EvaluationSettings>>().Value;

        try
        {
            logger.LogInformation("Starting Prompt Quality Evaluation Application");

            // Ensure test data exists
            if (!File.Exists(settings.TestDataPath))
            {
                logger.LogError($"Test data file not found: {settings.TestDataPath}" +
                    "Please ensure the test data file exists before running evaluation.");
                return;
            }

            // Run evaluation
            await evaluationService.RunEvaluationAsync(settings.TestDataPath);

            // Provide report generation instructions
            await evaluationService.GenerateReportAsync();

            logger.LogInformation("Application completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during evaluation");
        }
    }
}
