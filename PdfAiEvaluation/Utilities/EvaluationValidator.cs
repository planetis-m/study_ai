using PdfAiEvaluator.Configuration;
using PdfAiEvaluator.Validation;

namespace PdfAiEvaluator.Utilities;

public static class EvaluationValidator
{
    public static void ValidateEvaluationSettings(EvaluationSettings settings)
    {
        Guard.NotNullOrWhiteSpace(settings.TestDataPath, nameof(settings.TestDataPath));
        Guard.NotNullOrWhiteSpace(settings.StorageRootPath, nameof(settings.StorageRootPath));
        Guard.NotNullOrWhiteSpace(settings.ExecutionName, nameof(settings.ExecutionName));
        Guard.NotNullOrWhiteSpace(settings.EvaluatorProvider, nameof(settings.EvaluatorProvider));
        Guard.NotNullOrWhiteSpace(settings.EvaluatorModel, nameof(settings.EvaluatorModel));
        Guard.NotNullOrWhiteSpace(settings.TargetProvider, nameof(settings.TargetProvider));
        Guard.NotNullOrWhiteSpace(settings.TargetModel, nameof(settings.TargetModel));
        Guard.NotNull(settings.TargetOptions, nameof(settings.TargetOptions));

        if (!File.Exists(settings.TestDataPath))
        {
            throw new FileNotFoundException($"Test data file not found: {settings.TestDataPath}");
        }
    }
}
