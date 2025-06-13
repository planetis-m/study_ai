using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PdfAiEvaluator.Configuration;
using PdfAiEvaluator.Converters;
using PdfAiEvaluator.Validation;
using PdfAiEvaluator.Models;
using System.Text.Json;
using System.Threading;

namespace PdfAiEvaluator.Services;

public class EvaluationService : IEvaluationService
{
    private readonly IAiServiceFactory _aiServiceFactory;
    private readonly EvaluationSettings _settings;
    private readonly ILogger<EvaluationService> _logger;
    private ReportingConfiguration? _reportingConfiguration;

    public EvaluationService(
        IAiServiceFactory aiServiceFactory,
        IOptions<EvaluationSettings> settings,
        ILogger<EvaluationService> logger)
    {
        _aiServiceFactory = aiServiceFactory;
        _settings = Guard.NotNullOptions(settings, nameof(settings));
        _logger = logger;
    }

    public async Task RunEvaluationAsync(string testDataPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Starting evaluation run...");

        // Load test data
        var testSet = await LoadTestDataAsync(testDataPath, cancellationToken);

        // Create chat clients
        var targetChatClient = _aiServiceFactory.CreateChatClient(
            _settings.TargetProvider,
            _settings.TargetModel);

        var evaluatorChatClient = _aiServiceFactory.CreateChatClient(
            _settings.EvaluatorProvider,
            _settings.EvaluatorModel);

        // Create evaluators
        var evaluators = CreateEvaluators();

        // Create reporting configuration with disk-based storage
        _reportingConfiguration = DiskBasedReportingConfiguration.Create(
            storageRootPath: _settings.StorageRootPath,
            evaluators: evaluators,
            chatConfiguration: new ChatConfiguration(evaluatorChatClient),
            enableResponseCaching: _settings.EnableResponseCaching,
            timeToLiveForCacheEntries: TimeSpan.FromHours(_settings.TimeToLiveHours),
            executionName: _settings.ExecutionName,
            tags: ["prompt-quality", "evaluation"]
        );

        // Run evaluations for each test case with multiple iterations
        var evaluationTasks = new List<Task>();
        var semaphore = new SemaphoreSlim(_settings.MaxConcurrentRequests);

        foreach (var testCase in testSet.TestCases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("Starting evaluation for test case: {TestId}", testCase.TestId);

            // Run multiple iterations for better reliability
            for (int iteration = 1; iteration <= _settings.RequestsPerTestCase; iteration++)
            {
                var iterationNumber = iteration; // Capture for closure

                // Wait for a slot to become available
                await semaphore.WaitAsync(cancellationToken);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await using ScenarioRun scenarioRun = await _reportingConfiguration.CreateScenarioRunAsync(
                            scenarioName: testCase.TestId,
                            iterationName: iterationNumber.ToString(),
                            additionalTags: GetTagsForTestCase(testCase));

                        // Get model response using the target chat client
                        var modelResponse = await ExecuteWithTimeoutAsync(
                            async (ct) => await targetChatClient.GetResponseAsync(testCase.Messages, new ChatOptions(), ct),
                            TimeSpan.FromMinutes(5),
                            cancellationToken);

                        // Evaluate using all evaluators in the scenario run
                        var result = await ExecuteWithTimeoutAsync(
                            async (ct) => await scenarioRun.EvaluateAsync(
                                testCase.Messages,
                                modelResponse,
                                additionalContext: CreateAdditionalContextForScenario(testCase),
                                cancellationToken: ct),
                            TimeSpan.FromMinutes(3),
                            cancellationToken);

                        _logger.LogInformation(
                            "Completed evaluation: TestId={TestId}, Iteration={Iteration}",
                            testCase.TestId, iterationNumber);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error evaluating test case: {TestId}, Iteration: {Iteration}",
                            testCase.TestId, iterationNumber);
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

            _logger.LogInformation("Evaluation run completed. Results stored in: {StoragePath}",
                _settings.StorageRootPath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Evaluation run was cancelled. Partial results may be stored in: {StoragePath}",
                _settings.StorageRootPath);
            throw;
        }
    }

    public async Task GenerateReportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storagePath = Path.GetFullPath(_settings.StorageRootPath);
        var instructions = $"""
        Report generation instructions:
        1. Install the AI evaluation console tool:
            dotnet new tool-manifest
            dotnet tool install Microsoft.Extensions.AI.Evaluation.Console

        2. Generate HTML report:
            dotnet aieval report --path "{storagePath}" --output report.html --open
        """;

        _logger.LogInformation(instructions);
        await Task.CompletedTask;
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

        // Add context based on what's available in the test case
        if (!string.IsNullOrEmpty(testCase.GroundTruth))
        {
            // Add contexts that use ground truth
            context.Add(new CompletenessEvaluatorContext(testCase.GroundTruth));
            context.Add(new EquivalenceEvaluatorContext(testCase.GroundTruth));
        }

        if (!string.IsNullOrEmpty(testCase.GroundingContext))
        {
            // Add context for groundedness evaluation
            context.Add(new GroundednessEvaluatorContext(testCase.GroundingContext));
        }

        return context;
    }
}
