using System.Text.Json;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public static class ArchiveUtilities
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<PipelineArchive?> LoadArchiveAsync(string archiveFilePath)
    {
        if (!File.Exists(archiveFilePath))
            return null;

        try
        {
            var jsonContent = await File.ReadAllTextAsync(archiveFilePath);
            return JsonSerializer.Deserialize<PipelineArchive>(jsonContent, JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading archive {archiveFilePath}: {ex.Message}");
            return null;
        }
    }

    public static List<string> GetArchiveFilesForPdf(string baseArchiveDirectory, string pdfPath)
    {
        var archiveFiles = new List<string>();

        try
        {
            var pdfDirectoryName = CreateSanitizedDirectoryName(pdfPath);
            var pdfArchiveDirectory = Path.Combine(baseArchiveDirectory, pdfDirectoryName);

            if (Directory.Exists(pdfArchiveDirectory))
            {
                archiveFiles.AddRange(Directory.GetFiles(pdfArchiveDirectory, "*_PipelineResult.json"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting archive files for PDF {pdfPath}: {ex.Message}");
        }

        return archiveFiles.OrderByDescending(f => File.GetCreationTimeUtc(f)).ToList();
    }

    public static Task CleanupOldArchivesAsync(ArchiveSettings archiveSettings, string pdfPath)
    {
        if (archiveSettings.ArchiveRetentionDays <= 0 && archiveSettings.MaxArchivesPerPdf <= 0)
            return Task.CompletedTask;

        return Task.Run(() =>
        {
            try
            {
                var archiveFiles = GetArchiveFilesForPdf(archiveSettings.BaseArchiveDirectory, pdfPath);
                var filesToDelete = new List<string>();

                // Apply retention period cleanup
                if (archiveSettings.ArchiveRetentionDays > 0)
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-archiveSettings.ArchiveRetentionDays);
                    filesToDelete.AddRange(archiveFiles.Where(f => File.GetCreationTimeUtc(f) < cutoffDate));
                }

                // Apply max archives per PDF cleanup
                if (archiveSettings.MaxArchivesPerPdf > 0 && archiveFiles.Count > archiveSettings.MaxArchivesPerPdf)
                {
                    var excessFiles = archiveFiles.Skip(archiveSettings.MaxArchivesPerPdf);
                    filesToDelete.AddRange(excessFiles);
                }

                // Delete the files
                foreach (var fileToDelete in filesToDelete.Distinct())
                {
                    try
                    {
                        File.Delete(fileToDelete);
                        Console.WriteLine($"Deleted old archive: {Path.GetFileName(fileToDelete)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete archive {fileToDelete}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during archive cleanup: {ex.Message}");
            }
        });
    }

    private static string CreateSanitizedDirectoryName(string pdfPath)
    {
        // Use the same logic as PipelineArchiveManager
        if (string.IsNullOrWhiteSpace(pdfPath))
            return "unknown_pdf";

        var fileName = Path.GetFileNameWithoutExtension(pdfPath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return CreateHashDirectoryName(pdfPath);
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, "_+", "_");
        sanitized = sanitized.Trim('_');

        if (string.IsNullOrWhiteSpace(sanitized) || sanitized.Length > 100)
        {
            return CreateHashDirectoryName(pdfPath);
        }

        return sanitized;
    }

    private static string CreateHashDirectoryName(string pdfPath)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pdfPath));
        var hashString = Convert.ToHexString(hashBytes)[..16];
        return $"pdf_hash_{hashString.ToLowerInvariant()}";
    }
}
