namespace PdfAiEvaluator.Utilities;

public static class TagsHelper
{
    public interface IHasTags
    {
        List<string>? Tags { get; }
    }

    public static List<string> GetTags<T>(T testCase) where T : IHasTags
    {
        var tags = new List<string>();

        if (testCase.Tags != null)
        {
            foreach (var tag in testCase.Tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    tags.Add(tag);
                }
            }
        }

        return tags;
    }
}
