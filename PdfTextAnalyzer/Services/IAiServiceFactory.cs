using Microsoft.Extensions.AI;

namespace PdfTextAnalyzer.Services;

public interface IAiServiceFactory
{
    IChatClient CreateChatClient(string provider, string model);
}
