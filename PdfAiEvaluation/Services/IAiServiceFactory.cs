using Microsoft.Extensions.AI;

namespace PdfAiEvaluator.Services;

public interface IAiServiceFactory
{
    IChatClient CreateChatClient(string provider, string model);
}
