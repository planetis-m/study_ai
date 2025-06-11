using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public abstract class AiServiceBase
{
    protected readonly IAiServiceFactory _aiServiceFactory;
    protected readonly AiSettings _aiSettings;

    protected AiServiceBase(IAiServiceFactory aiServiceFactory, IOptions<AiSettings> aiSettings)
    {
        _aiServiceFactory = aiServiceFactory ?? throw new ArgumentNullException(nameof(aiServiceFactory));
        _aiSettings = aiSettings.Value ?? throw new ArgumentNullException(nameof(aiSettings));
    }

    protected async Task<string> CallAiServiceAsync(
        string systemMessage,
        string userMessage,
        ModelSettings modelSettings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(systemMessage))
            throw new ArgumentException("System message cannot be null or empty", nameof(systemMessage));

        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("User message cannot be null or empty", nameof(userMessage));

        if (modelSettings == null)
            throw new ArgumentNullException(nameof(modelSettings));

        // Create chat client for this specific model/provider combination
        var chatClient = _aiServiceFactory.CreateChatClient(modelSettings.Provider, modelSettings.ModelName);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemMessage),
            new(ChatRole.User, userMessage)
        };

        var options = new ChatOptions
        {
            MaxOutputTokens = modelSettings.MaxTokens,
            Temperature = modelSettings.Temperature,
            TopP = modelSettings.TopP,
        };

        var response = await chatClient.GetResponseAsync(messages, options, cancellationToken);

        if (response == null || string.IsNullOrWhiteSpace(response.Text))
        {
            throw new InvalidOperationException($"({modelSettings.Provider}) returned empty response");
        }

        return response.Text;
    }
}
