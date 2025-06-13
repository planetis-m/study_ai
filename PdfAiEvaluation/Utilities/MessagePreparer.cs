using Microsoft.Extensions.AI;
using PdfAiEvaluator.Models;

namespace PdfAiEvaluator.Utilities;

public static class MessagePreparer
{
    public static List<ChatMessage> PrepareMessagesForTarget(EvaluationTestSet testSet, EvaluationTestData testCase)
    {
        var messages = PrepareMessagesForEvaluation(testSet, testCase);

        if (!string.IsNullOrEmpty(testCase.GroundTruth))
        {
            messages.Add(new ChatMessage(ChatRole.User, testCase.GroundTruth));
        }
        else if (!string.IsNullOrEmpty(testCase.GroundingContext))
        {
            messages.Add(new ChatMessage(ChatRole.User, testCase.GroundingContext));
        }

        return messages;
    }

    public static List<ChatMessage> PrepareMessagesForEvaluation(EvaluationTestSet testSet, EvaluationTestData testCase)
    {
        var messages = new List<ChatMessage>();

        if (testSet.Messages?.Count > 0)
        {
            messages.AddRange(testSet.Messages);
        }

        if (testCase.Messages?.Count > 0)
        {
            messages.AddRange(testCase.Messages);
        }

        return messages;
    }
}
