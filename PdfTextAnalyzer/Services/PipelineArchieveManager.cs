using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PdfTextAnalyzer.Models;

namespace PdfTextAnalyzer.Services;

public static class PipelineArchiveManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task ArchiveResultAsync(
        PipelineResult result,
        object configuration,
        string baseArchiveDirectory)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(baseArchiveDirectory))
            throw new ArgumentException("Base archive directory cannot be null or empty", nameof(baseArchiveDirectory));

        try
        {
            // Generate unique run identifier and timestamp
            var runId = Guid.NewGuid();
            var executionTimestamp = DateTime.UtcNow;
            var timestampString = executionTimestamp.ToString("yyyyMMddHHmmssfff");

            // Create sanitized directory name for the PDF
            var pdfDirectoryName = CreateSanitizedDirectoryName(result.PdfPath);
            var pdfArchiveDirectory = Path.Combine(baseArchiveDirectory, pdfDirectoryName);

            // Ensure the archive directory exists
            Directory.CreateDirectory(pdfArchiveDirectory);

            // Create the archive filename
            var archiveFileName = $"{timestampString}_{runId:N}_PipelineResult.json";
            var archiveFilePath = Path.Combine(pdfArchiveDirectory, archiveFileName);

            // Create the archive object
            var archiveData = new PipelineArchive
            {
                RunMetadata = new RunMetadata
                {
                    RunId = runId,
                    ExecutionTimestampUtc = executionTimestamp,
                    SourcePdfPath = result.PdfPath,
                    AppSettingsUsed = configuration
                },
                PipelineData = result
            };

            // Serialize and write to file
            var jsonContent = JsonSerializer.Serialize(archiveData, JsonOptions);
            await File.WriteAllTextAsync(archiveFilePath, jsonContent);

            Console.WriteLine($"Pipeline result archived successfully:");
            Console.WriteLine($"  Archive File: {archiveFilePath}");
            Console.WriteLine($"  Run ID: {runId}");
            Console.WriteLine($"  Timestamp: {executionTimestamp:yyyy-MM-dd HH:mm:ss.fff} UTC");
        }
        catch (UnauthorizedAccessException authEx)
        {
            Console.WriteLine($"Access denied when creating archive: {authEx.Message}");
            throw;
        }
        catch (DirectoryNotFoundException dirEx)
        {
            Console.WriteLine($"Directory not found when creating archive: {dirEx.Message}");
            throw;
        }
        catch (IOException ioEx)
        {
            Console.WriteLine($"Error writing archive file: {ioEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error during archiving: {ex.Message}");
            throw;
        }
    }

    private static string CreateSanitizedDirectoryName(string pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
            return "unknown_pdf";

        // Extract filename without extension
        var fileName = Path.GetFileNameWithoutExtension(pdfPath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            // If no valid filename, create hash of full path
            return CreateHashDirectoryName(pdfPath);
        }

        // Remove invalid characters for directory names
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Replace multiple consecutive underscores with single underscore
        sanitized = Regex.Replace(sanitized, "_+", "_");

        // Trim leading/trailing underscores
        sanitized = sanitized.Trim('_');

        // If sanitized name is empty or too long, create hash-based name
        if (string.IsNullOrWhiteSpace(sanitized) || sanitized.Length > 100)
        {
            return CreateHashDirectoryName(pdfPath);
        }

        return sanitized;
    }

    private static string CreateHashDirectoryName(string pdfPath)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pdfPath));
        var hashString = Convert.ToHexString(hashBytes)[..16]; // Take first 16 characters
        return $"pdf_hash_{hashString.ToLowerInvariant()}";
    }
}

public class PipelineArchive
{
    public RunMetadata RunMetadata { get; set; } = new();
    public PipelineResult PipelineData { get; set; } = new();
}

public class RunMetadata
{
    public Guid RunId { get; set; }
    public DateTime ExecutionTimestampUtc { get; set; }
    public string SourcePdfPath { get; set; } = string.Empty;
    public object AppSettingsUsed { get; set; } = new();
}
