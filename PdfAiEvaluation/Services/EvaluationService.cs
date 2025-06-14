using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Logging;
using PdfAiEvaluator.Configuration;
using PdfAiEvaluator.Models;
using PdfAiEvaluator.Utilities;

namespace PdfAiEvaluator.Services;

public class EvaluationService : IEvaluationService
{
    private readonly IAiServiceFactory _aiServiceFactory;
    private readonly ILogger<EvaluationService> _logger;

    public EvaluationService(
        IAiServiceFactory aiServiceFactory,
        ILogger<EvaluationService> logger)
    {
        _aiServiceFactory = aiServiceFactory;
        _logger = logger;
    }

    public async Task RunEvaluationAsync(EvaluationSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Starting evaluation run for: {EvaluationName}", settings.ExecutionName);

        // Validate settings
        EvaluationValidator.ValidateEvaluationSettings(settings);

        // Load test data
        var testSet = await TestDataLoader.LoadTestDataAsync(settings.TestDataPath, cancellationToken);

        // Create chat clients
        var targetChatClient = _aiServiceFactory.CreateChatClient(
            settings.TargetProvider,
            settings.TargetModel);

        var evaluatorChatClient = _aiServiceFactory.CreateChatClient(
            settings.EvaluatorProvider,
            settings.EvaluatorModel);

        // Create evaluators based on the test set configuration
        var evaluators = EvaluationFactory.CreateEvaluators(testSet.Evaluators);

        // Validate that evaluators are configured
        if (evaluators.Count == 0)
        {
            throw new InvalidOperationException($"No evaluators configured for '{settings.ExecutionName}'. Please specify evaluators in the test data.");
        }

        _logger.LogInformation("Using evaluators: {Evaluators} for: {EvaluationName}",
            string.Join(", ", testSet.Evaluators), settings.ExecutionName);

        // Create reporting configuration with disk-based storage
        var reportingConfiguration = DiskBasedReportingConfiguration.Create(
            storageRootPath: settings.StorageRootPath,
            evaluators: evaluators,
            chatConfiguration: new ChatConfiguration(evaluatorChatClient),
            enableResponseCaching: settings.EnableResponseCaching,
            timeToLiveForCacheEntries: TimeSpan.FromHours(settings.TimeToLiveHours),
            executionName: settings.ExecutionName,
            tags: TagsHelper.GetTags(testSet)
        );

        // Run evaluations for each test case with multiple iterations
        var evaluationTasks = new List<Task>();
        var semaphore = new SemaphoreSlim(settings.MaxConcurrentEvaluations);

        foreach (var testCase in testSet.TestCases)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scenarioName = $"{testSet.Name}.{testCase.TestId}";
            _logger.LogInformation("Starting evaluation for scenario: {ScenarioName} in {EvaluationName}",
                scenarioName, settings.ExecutionName);

            // Run multiple iterations for better reliability
            for (int iteration = 1; iteration <= settings.IterationsPerTestCase; iteration++)
            {
                var iterationNumber = iteration; // Capture for closure

                // Wait for a slot to become available
                await semaphore.WaitAsync(cancellationToken);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await using ScenarioRun scenarioRun = await reportingConfiguration.CreateScenarioRunAsync(
                            scenarioName: scenarioName,
                            iterationName: iterationNumber.ToString(),
                            additionalTags: TagsHelper.GetTags(testCase));

                        // Prepare messages for the target chat client (including additional context)
                        var messagesForTarget = MessagePreparer.PrepareMessagesForTarget(testSet, testCase);

                        // Get model response using the target chat client
                        var modelResponse = await TimeoutHelper.ExecuteWithTimeoutAsync(
                            async (ct) => await targetChatClient.GetResponseAsync(messagesForTarget, settings.TargetOptions, ct),
                            TimeSpan.FromMinutes(settings.ModelResponseTimeout),
                            cancellationToken);

                        // Prepare messages for evaluation (without additional context to save tokens)
                        var messagesForEvaluation = MessagePreparer.PrepareMessagesForEvaluation(testSet, testCase);

                        // Evaluate using all evaluators in the scenario run
                        var result = await TimeoutHelper.ExecuteWithTimeoutAsync(
                            async (ct) => await scenarioRun.EvaluateAsync(
                                messagesForEvaluation,
                                modelResponse,
                                additionalContext: EvaluationFactory.CreateAdditionalContextForScenario(testCase),
                                cancellationToken: ct),
                            TimeSpan.FromMinutes(settings.EvaluationTimeout),
                            cancellationToken);

                        _logger.LogInformation(
                            "Completed evaluation: Scenario={ScenarioName}, Iteration={Iteration}, Evaluation={EvaluationName}",
                            scenarioName, iterationNumber, settings.ExecutionName);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error evaluating Scenario: {ScenarioName}, Iteration: {Iteration}, Evaluation: {EvaluationName}",
                            scenarioName, iterationNumber, settings.ExecutionName);
                    }
                    finally
                    {
                        semaphore.Release(); // Release the semaphore slot
                    }
                }, cancellationToken);

                evaluationTasks.Add(task);
            }
        }

        try
        {
            // Wait for all evaluations to complete
            await Task.WhenAll(evaluationTasks);

            _logger.LogInformation("Evaluation run completed for {EvaluationName}. Results stored in: {StoragePath}",
                settings.ExecutionName, settings.StorageRootPath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Evaluation run was cancelled for {EvaluationName}. Partial results may be stored in: {StoragePath}",
                settings.ExecutionName, settings.StorageRootPath);
            throw;
        }
    }

    public async Task RunAllEvaluationsAsync(IEnumerable<EvaluationSettings> evaluations, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting all evaluations. Total count: {Count}", evaluations.Count());

        foreach (var evaluation in evaluations)
        {
            try
            {
                await RunEvaluationAsync(evaluation, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Evaluation sequence was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run evaluation: {EvaluationName}", evaluation.ExecutionName);
                // Continue with other evaluations even if one fails
            }
        }

        _logger.LogInformation("All evaluations completed");
    }
}
