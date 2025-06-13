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
                services.Configure<EvaluationsConfiguration>(configuration.GetSection(EvaluationsConfiguration.SectionName));

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
        var evaluationsConfig = host.Services.GetRequiredService<IOptions<EvaluationsConfiguration>>().Value;

        try
        {
            logger.LogInformation("Starting Prompt Quality Evaluation Application");

            // Check if we have any evaluations configured
            if (!evaluationsConfig.HasEvaluations)
            {
                logger.LogError("No evaluations found in configuration. Please ensure the 'Evaluations' section contains at least one evaluation.");
                return ExitCode.Failure;
            }

            logger.LogInformation("Found {Count} evaluation(s) configured", evaluationsConfig.Count);

            // Check for command line arguments to run specific evaluation
            if (args.Length > 0)
            {
                var evaluationName = args[0];
                var specificEvaluation = evaluationsConfig.GetEvaluationByName(evaluationName);

                if (specificEvaluation == null)
                {
                    logger.LogError("Evaluation '{EvaluationName}' not found. Available evaluations: {AvailableEvaluations}",
                        evaluationName,
                        string.Join(", ", evaluationsConfig.Select(e => e.ExecutionName)));
                    return ExitCode.Failure;
                }

                logger.LogInformation("Running specific evaluation: {EvaluationName}", evaluationName);

                // Validate test data exists
                if (!File.Exists(specificEvaluation.TestDataPath))
                {
                    logger.LogError("Test data file not found for evaluation '{EvaluationName}': {TestDataPath}",
                        specificEvaluation.ExecutionName, specificEvaluation.TestDataPath);
                    return ExitCode.Failure;
                }

                // Run specific evaluation
                await evaluationService.RunEvaluationAsync(specificEvaluation, cts.Token);

                // Generate report for this specific evaluation
                ReportGenerator.GenerateReport(specificEvaluation.StorageRootPath, logger);
            }
            else
            {
                logger.LogInformation("Running all evaluations");

                // Validate all test data files exist
                var missingFiles = evaluationsConfig
                    .Where(e => !File.Exists(e.TestDataPath))
                    .ToList();

                if (missingFiles.Any())
                {
                    foreach (var evaluation in missingFiles)
                    {
                        logger.LogError("Test data file not found for evaluation '{EvaluationName}': {TestDataPath}",
                            evaluation.ExecutionName, evaluation.TestDataPath);
                    }
                    return ExitCode.Failure;
                }

                // Run all evaluations
                await evaluationService.RunAllEvaluationsAsync(evaluationsConfig, cts.Token);

                // Generate report
                ReportGenerator.GenerateReportTemplate(logger);
            }

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
