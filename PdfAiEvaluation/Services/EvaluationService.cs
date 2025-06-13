using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Logging;
using PdfAiEvaluator.Configuration;
using PdfAiEvaluator.Converters;
using PdfAiEvaluator.Validation;
using PdfAiEvaluator.Models;
using System.Text.Json;

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
        ValidateEvaluationSettings(settings);

        // Load test data
        var testSet = await LoadTestDataAsync(settings.TestDataPath, cancellationToken);

        // Create chat clients
        var targetChatClient = _aiServiceFactory.CreateChatClient(
            settings.TargetProvider,
            settings.TargetModel);

        var evaluatorChatClient = _aiServiceFactory.CreateChatClient(
            settings.EvaluatorProvider,
            settings.EvaluatorModel);

        // Create evaluators
        var evaluators = CreateEvaluators();

        // Create reporting configuration with disk-based storage
        var reportingConfiguration = DiskBasedReportingConfiguration.Create(
            storageRootPath: settings.StorageRootPath,
            evaluators: evaluators,
            chatConfiguration: new ChatConfiguration(evaluatorChatClient),
            enableResponseCaching: settings.EnableResponseCaching,
            timeToLiveForCacheEntries: TimeSpan.FromHours(settings.TimeToLiveHours),
            executionName: settings.ExecutionName,
            tags: ["prompt-quality", "evaluation"]
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
                            additionalTags: GetTagsForTestCase(testCase));

                        // Create chat options using the evaluation's ChatSettings
                        var chatOptions = new ChatOptions
                        {
                            MaxOutputTokens = settings.TargetSettings.MaxOutputTokens,
                            Temperature = settings.TargetSettings.Temperature,
                            TopP = settings.TargetSettings.TopP,
                            FrequencyPenalty = settings.TargetSettings.FrequencyPenalty,
                            PresencePenalty = settings.TargetSettings.PresencePenalty
                        };

                        // Prepare messages for the target chat client (including additional context)
                        var messagesForTarget = PrepareMessagesForTarget(testSet, testCase);

                        // Get model response using the target chat client
                        var modelResponse = await ExecuteWithTimeoutAsync(
                            async (ct) => await targetChatClient.GetResponseAsync(messagesForTarget, chatOptions, ct),
                            TimeSpan.FromMinutes(5),
                            cancellationToken);

                        // Prepare messages for evaluation (without additional context to save tokens)
                        var messagesForEvaluation = PrepareMessagesForEvaluation(testCase);

                        // Evaluate using all evaluators in the scenario run
                        var result = await ExecuteWithTimeoutAsync(
                            async (ct) => await scenarioRun.EvaluateAsync(
                                messagesForEvaluation,
                                modelResponse,
                                additionalContext: CreateAdditionalContextForScenario(testCase),
                                cancellationToken: ct),
                            TimeSpan.FromMinutes(5),
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

        var storagePaths = new List<string>();

        foreach (var evaluation in evaluations)
        {
            try
            {
                await RunEvaluationAsync(evaluation, cancellationToken);
                storagePaths.Add(evaluation.StorageRootPath);
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

    public async Task GenerateReportAsync(string storagePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullStoragePath = Path.GetFullPath(storagePath);
        var instructions = $"""
        Report generation instructions:
        1. Install the AI evaluation console tool:
            dotnet new tool-manifest
            dotnet tool install Microsoft.Extensions.AI.Evaluation.Console

        2. Generate HTML report:
            dotnet aieval report --path "{fullStoragePath}" --output report.html --open
        """;

        _logger.LogInformation(instructions);
        await Task.CompletedTask;
    }

    private void ValidateEvaluationSettings(EvaluationSettings settings)
    {
        Guard.NotNullOrWhiteSpace(settings.TestDataPath, nameof(settings.TestDataPath));
        Guard.NotNullOrWhiteSpace(settings.StorageRootPath, nameof(settings.StorageRootPath));
        Guard.NotNullOrWhiteSpace(settings.ExecutionName, nameof(settings.ExecutionName));
        Guard.NotNullOrWhiteSpace(settings.EvaluatorProvider, nameof(settings.EvaluatorProvider));
        Guard.NotNullOrWhiteSpace(settings.EvaluatorModel, nameof(settings.EvaluatorModel));
        Guard.NotNullOrWhiteSpace(settings.TargetProvider, nameof(settings.TargetProvider));
        Guard.NotNullOrWhiteSpace(settings.TargetModel, nameof(settings.TargetModel));
        Guard.NotNull(settings.TargetSettings, nameof(settings.TargetSettings));

        if (!File.Exists(settings.TestDataPath))
        {
            throw new FileNotFoundException($"Test data file not found: {settings.TestDataPath}");
        }
    }

    private async Task<T> ExecuteWithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await operation(combinedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {timeout.TotalMinutes} minutes");
        }
    }

    private async Task<EvaluationTestSet> LoadTestDataAsync(string testDataPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(testDataPath))
        {
            throw new FileNotFoundException($"Test data file not found: {testDataPath}");
        }

        var jsonContent = await File.ReadAllTextAsync(testDataPath, cancellationToken);
        var testSet = JsonSerializer.Deserialize<EvaluationTestSet>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new ChatMessageJsonConverter() }
        });

        return testSet ?? throw new InvalidOperationException("Failed to deserialize test data");
    }

    private List<IEvaluator> CreateEvaluators()
    {
        return new List<IEvaluator>
        {
            new CompletenessEvaluator(),
            new EquivalenceEvaluator(),
            new GroundednessEvaluator()
        };
    }

    public static List<string> GetTagsForTestCase(EvaluationTestData testCase)
    {
        var tags = new List<string>();

        if (testCase.Tags != null)
        {
            foreach (var tag in testCase.Tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    tags.Add(tag);
                }
            }
        }

        return tags;
    }

    private List<EvaluationContext> CreateAdditionalContextForScenario(EvaluationTestData testCase)
    {
        var context = new List<EvaluationContext>();

        if (!string.IsNullOrEmpty(testCase.GroundTruth))
        {
            // Add context that uses ground truth
            context.Add(new CompletenessEvaluatorContext(testCase.GroundTruth));
        }

        if (!string.IsNullOrEmpty(testCase.GoldenAnswer))
        {
            // Add context that uses reference answer
            context.Add(new EquivalenceEvaluatorContext(testCase.GoldenAnswer));
        }

        if (!string.IsNullOrEmpty(testCase.GroundingContext))
        {
            // Add context for groundedness evaluation
            context.Add(new GroundednessEvaluatorContext(testCase.GroundingContext));
        }

        return context;
    }

    private List<ChatMessage> PrepareMessagesForTarget(EvaluationTestSet testSet, EvaluationTestData testCase)
    {
        var messages = new List<ChatMessage>();

        if (testSet.Messages?.Count > 0)
        {
            messages.AddRange(testSet.Messages);
        }

        if (testCase.Messages?.Count > 0)
        {
            messages.AddRange(testCase.Messages);
        }

        if (!string.IsNullOrEmpty(testCase.GroundTruth))
        {
            messages.Add(new ChatMessage(ChatRole.User, testCase.GroundTruth));
        }
        else if (!string.IsNullOrEmpty(testCase.GroundingContext))
        {
            messages.Add(new ChatMessage(ChatRole.User, testCase.GroundingContext));
        }

        return messages;
    }

    private List<ChatMessage> PrepareMessagesForEvaluation(EvaluationTestData testCase)
    {
        var messages = new List<ChatMessage>();

        if (testSet.Messages?.Count > 0)
        {
            messages.AddRange(testSet.Messages);
        }

        if (testCase.Messages?.Count > 0)
        {
            messages.AddRange(testCase.Messages);
        }

        return messages;
    }
}
