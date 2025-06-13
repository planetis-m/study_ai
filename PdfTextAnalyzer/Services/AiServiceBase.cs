using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Validation;
using PdfTextAnalyzer.Utilities;

namespace PdfTextAnalyzer.Services;

public abstract class AiServiceBase
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    protected readonly IAiServiceFactory _aiServiceFactory;

    protected AiServiceBase(IAiServiceFactory aiServiceFactory)
    {
        _aiServiceFactory = Guard.NotNull(aiServiceFactory, nameof(aiServiceFactory));
    }

    protected async Task<string> CallAiServiceAsync(
        ModelSettings settings,
        CancellationToken cancellationToken)
    {
        Guard.NotNull(settings, nameof(settings));
        Guard.NotNullOrWhiteSpace(settings.SystemMessage, nameof(settings.SystemMessage));
        Guard.NotNullOrWhiteSpace(settings.TaskPrompt, nameof(settings.TaskPrompt));
        Guard.NotNullOrWhiteSpace(settings.Provider, nameof(settings.Provider));
        Guard.NotNullOrWhiteSpace(settings.ModelName, nameof(settings.ModelName));

        // Create chat client for this specific model/provider combination
        var chatClient = _aiServiceFactory.CreateChatClient(settings.Provider, settings.ModelName);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, settings.SystemMessage),
            new(ChatRole.User, settings.TaskPrompt)
        };

        var response = await TimeoutHelper.ExecuteWithTimeoutAsync(
            async (ct) => await chatClient.GetResponseAsync(messages, settings.Options, ct),
            DefaultTimeout,
            cancellationToken);

        var message = response.Text;

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidOperationException($"({settings.Provider}) returned empty response");
        }

        return message;
    }
}
