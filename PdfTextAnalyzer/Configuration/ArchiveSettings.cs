namespace PdfTextAnalyzer.Configuration;

public class ArchiveSettings
{
    public const string SectionName = "Archive";

    public bool EnableArchiving { get; set; } = false;
    public string BaseArchiveDirectory { get; set; } = "Archives";
    public int MaxArchivesPerPdf { get; set; } = 0;
    public int ArchiveRetentionDays { get; set; } = 0;
}
