using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PdfAiEvaluator.Converters;
using PdfAiEvaluator.Configuration;
using PdfAiEvaluator.Models;
using System.Text.Json;

namespace PdfAiEvaluator.Services;

public interface IEvaluationService
{
    Task RunEvaluationAsync(string testDataPath);
    Task GenerateReportAsync();
}

public class EvaluationService : IEvaluationService
{
    private readonly IAiServiceFactory _aiServiceFactory;
    private readonly EvaluationSettings _evaluationSettings;
    private readonly ILogger<EvaluationService> _logger;
    private ReportingConfiguration? _reportingConfiguration;

    public EvaluationService(
        IAiServiceFactory aiServiceFactory,
        IOptions<EvaluationSettings> evaluationSettings,
        ILogger<EvaluationService> logger)
    {
        _aiServiceFactory = aiServiceFactory;
        _evaluationSettings = evaluationSettings.Value;
        _logger = logger;
    }

    public async Task RunEvaluationAsync(string testDataPath)
    {
        _logger.LogInformation("Starting evaluation run...");

        // Load test data
        var testSet = await LoadTestDataAsync(testDataPath);

        // Create chat clients
        var targetChatClient = _aiServiceFactory.CreateChatClient(
            _evaluationSettings.TargetProvider,
            _evaluationSettings.TargetModel);

        var evaluatorChatClient = _aiServiceFactory.CreateChatClient(
            _evaluationSettings.EvaluatorProvider,
            _evaluationSettings.EvaluatorModel);

        // Create evaluators
        var evaluators = CreateEvaluators();

        // Create reporting configuration with disk-based storage
        _reportingConfiguration = DiskBasedReportingConfiguration.Create(
            storageRootPath: _evaluationSettings.StorageRootPath,
            evaluators: evaluators,
            chatConfiguration: new ChatConfiguration(evaluatorChatClient),
            enableResponseCaching: _evaluationSettings.EnableResponseCaching,
            timeToLiveForCacheEntries: TimeSpan.FromHours(_evaluationSettings.TimeToLiveHours),
            executionName: _evaluationSettings.ExecutionName,
            tags: ["prompt-quality", "evaluation", DateTime.UtcNow.ToString("yyyy-MM-dd")]
        );

        // Run evaluations for each test case with multiple iterations
        var evaluationTasks = new List<Task>();

        foreach (var testCase in testSet.TestCases)
        {
            _logger.LogInformation("Starting evaluation for test case: {TestId}", testCase.TestId);

            // Run multiple iterations in parallel for better reliability
            for (int iteration = 1; iteration <= 3; iteration++)
            {
                var iterationNumber = iteration; // Capture for closure

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await using ScenarioRun scenarioRun = await _reportingConfiguration.CreateScenarioRunAsync(
                            scenarioName: testCase.TestId,
                            iterationName: iterationNumber.ToString(),
                            additionalTags: GetTagsForTestCase(testCase));

                        // Get model response using the target chat client
                        var modelResponse = await targetChatClient.GetResponseAsync(testCase.Messages);

                        // Evaluate using all evaluators in the scenario run
                        var result = await scenarioRun.EvaluateAsync(
                            testCase.Messages,
                            modelResponse,
                            additionalContext: CreateAdditionalContextForScenario(testCase));

                        _logger.LogInformation(
                            "Completed evaluation: TestId={TestId}, Iteration={Iteration}",
                            testCase.TestId, iterationNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error evaluating test case: {TestId}, Iteration: {Iteration}",
                            testCase.TestId, iterationNumber);
                    }
                });

                evaluationTasks.Add(task);
            }
        }

        // Wait for all evaluations to complete
        await Task.WhenAll(evaluationTasks);

        _logger.LogInformation("Evaluation run completed. Results stored in: {StoragePath}",
            _evaluationSettings.StorageRootPath);
    }

    public async Task GenerateReportAsync()
    {
        _logger.LogInformation("Report generation instructions:");
        _logger.LogInformation("1. Install the AI evaluation console tool:");
        _logger.LogInformation("   dotnet tool install Microsoft.Extensions.AI.Evaluation.Console");
        _logger.LogInformation("2. Generate HTML report:");
        _logger.LogInformation("   dotnet aieval report --path \"{0}\" --output report.html",
            Path.GetFullPath(_evaluationSettings.StorageRootPath));

        await Task.CompletedTask;
    }

    private async Task<EvaluationTestSet> LoadTestDataAsync(string testDataPath)
    {
        if (!File.Exists(testDataPath))
        {
            throw new FileNotFoundException($"Test data file not found: {testDataPath}");
        }

        var jsonContent = await File.ReadAllTextAsync(testDataPath);
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

    private List<string> GetTagsForTestCase(EvaluationTestData testCase)
    {
        var tags = new List<string> { "prompt-quality", "evaluation" };

        // Add metadata as tags
        if (testCase.Metadata.TryGetValue("topic", out var topic) && topic is string topicStr)
        {
            tags.Add($"topic:{topicStr}");
        }

        if (testCase.Metadata.TryGetValue("complexity", out var complexity) && complexity is string complexityStr)
        {
            tags.Add($"complexity:{complexityStr}");
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
