using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Validation;
using PdfTextAnalyzer.Utilities;

namespace PdfTextAnalyzer.Services;

public abstract class AiServiceBase
{
    protected readonly IAiServiceFactory _aiServiceFactory;

    protected AiServiceBase(IAiServiceFactory aiServiceFactory)
    {
        _aiServiceFactory = Guard.NotNull(aiServiceFactory, nameof(aiServiceFactory));
    }

    protected async Task<string> CallAiServiceAsync(
        string systemMessage,
        string userMessage,
        ModelSettings modelSettings,
        CancellationToken cancellationToken)
    {
        Guard.NotNullOrWhiteSpace(systemMessage, nameof(systemMessage));
        Guard.NotNullOrWhiteSpace(userMessage, nameof(userMessage));
        Guard.NotNull(modelSettings, nameof(modelSettings));

        // Create chat client for this specific model/provider combination
        var chatClient = _aiServiceFactory.CreateChatClient(modelSettings.Provider, modelSettings.ModelName);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemMessage),
            new(ChatRole.User, userMessage)
        };

        var response = await TimeoutHelper.ExecuteWithTimeoutAsync(
            async (ct) => await chatClient.GetResponseAsync(messages, modelSettings.Options, ct),
            TimeSpan.FromMinutes(5),
            cancellationToken);

        var message = response.Text;

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidOperationException($"({modelSettings.Provider}) returned empty response");
        }

        return message;
    }
}
