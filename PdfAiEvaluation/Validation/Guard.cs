using Microsoft.Extensions.Options;

namespace PdfAiEvaluator.Validation;

public static class Guard
{
    public static void NotNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} cannot be null or empty", paramName);
    }

    public static T NotNull<T>(T? value, string paramName) where T : class
    {
        return value ?? throw new ArgumentNullException(paramName);
    }

    public static T NotNullOptions<T>(IOptions<T>? options, string paramName) where T : class
    {
        return options?.Value ?? throw new ArgumentNullException(paramName);
    }
}
