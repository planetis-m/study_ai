using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfAiEvaluator.Configuration;
using PdfAiEvaluator.Services;

namespace PdfAiEvaluator;

internal static class ExitCode
{
    public const int Success = 0;
    public const int Failure = 1;
}

class Program
{
    static async Task<int> Main(string[] args)
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
                logger.LogError("Test data file not found: {TestDataPath}. " +
                    "Please ensure the test data file exists before running evaluation.", settings.TestDataPath);
                return ExitCode.Failure;
            }

            // Run evaluation with cancellation token
            await evaluationService.RunEvaluationAsync(settings.TestDataPath, cts.Token);

            // Provide report generation instructions
            await evaluationService.GenerateReportAsync(cts.Token);

            logger.LogInformation("Application completed successfully");
            return ExitCode.Success;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Application was cancelled by user");
            return ExitCode.Failure;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during evaluation");
            return ExitCode.Failure;
        }
    }
}
