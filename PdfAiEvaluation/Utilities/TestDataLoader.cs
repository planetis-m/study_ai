using PdfAiEvaluator.Converters;
using PdfAiEvaluator.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PdfAiEvaluator.Utilities;

public static class TestDataLoader
{
    public static async Task<EvaluationTestSet> LoadTestDataAsync(string testDataPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(testDataPath))
        {
            throw new FileNotFoundException($"Test data file not found: {testDataPath}");
        }

        var jsonContent = await File.ReadAllTextAsync(testDataPath, cancellationToken);
        var testSet = JsonSerializer.Deserialize<EvaluationTestSet>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(), new ChatMessageJsonConverter() }
        });

        return testSet ?? throw new InvalidOperationException("Failed to deserialize test data");
    }
}
