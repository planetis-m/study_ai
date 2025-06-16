using System.ComponentModel.DataAnnotations;

namespace PdfTextAnalyzer.Configuration;

public class ArchiveSettings
{
    public const string SectionName = "Archive";

    [Required]
    public string BaseArchiveDirectory { get; set; } = "archives";
}
