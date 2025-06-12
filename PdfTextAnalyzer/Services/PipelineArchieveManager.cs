using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PdfTextAnalyzer.Models;
using PdfTextAnalyzer.Configuration;

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
        ApplicationSettings configuration,
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

            // Create archive configuration with model settings for active stages
            var archiveConfig = CreateArchiveConfiguration(configuration);

            // Create the archive object
            var archiveData = new PipelineArchive
            {
                RunMetadata = new RunMetadata
                {
                    RunId = runId,
                    ExecutionTimestampUtc = executionTimestamp,
                    SourcePdfPath = result.PdfPath,
                    Configuration = archiveConfig
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
            Console.WriteLine($"  Config Hash: {archiveConfig.ConfigurationHash}");
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

    private static ArchiveConfiguration CreateArchiveConfiguration(ApplicationSettings settings)
    {
        var config = new ArchiveConfiguration
        {
            PdfExtraction = settings.PdfExtraction,
            Pipeline = settings.Pipeline
        };

        // Only include model settings for stages that are enabled
        if (settings.Pipeline.Preprocessing)
        {
            config.PreprocessingModel = settings.Preprocessing.Model;
            config.PreprocessingSystemMessage = settings.Preprocessing.SystemMessage;
            config.PreprocessingTaskPrompt = settings.Preprocessing.TaskPrompt;
        }

        if (settings.Pipeline.Analysis)
        {
            config.AnalysisModel = settings.Analysis.Model;
            config.AnalysisSystemMessage = settings.Analysis.SystemMessage;
            config.AnalysisTaskPrompt = settings.Analysis.TaskPrompt;
        }

        // Create hash of all the configuration for integrity checking
        config.ConfigurationHash = CreateConfigurationHash(config);

        return config;
    }

    private static string CreateConfigurationHash(ArchiveConfiguration config)
    {
        // Create a copy without the hash for hashing (to avoid circular reference)
        var configForHashing = new
        {
            config.PdfExtraction,
            config.Pipeline,
            config.PreprocessingModel,
            config.PreprocessingSystemMessage,
            config.PreprocessingTaskPrompt,
            config.AnalysisModel,
            config.AnalysisSystemMessage,
            config.AnalysisTaskPrompt
        };

        // Serialize to JSON for consistent hashing
        var configJson = JsonSerializer.Serialize(configForHashing, JsonOptions);

        // Create SHA256 hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(configJson));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
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
    public ArchiveConfiguration Configuration { get; set; } = new();
}

public class ArchiveConfiguration
{
    public string ConfigurationHash { get; set; } = string.Empty;

    // Always included settings
    public PdfExtractionSettings PdfExtraction { get; set; } = new();
    public PipelineSettings Pipeline { get; set; } = new();

    // Preprocessing settings (only if preprocessing is enabled)
    public ModelSettings? PreprocessingModel { get; set; }
    public string? PreprocessingSystemMessage { get; set; }
    public string? PreprocessingTaskPrompt { get; set; }

    // Analysis settings (only if analysis is enabled)
    public ModelSettings? AnalysisModel { get; set; }
    public string? AnalysisSystemMessage { get; set; }
    public string? AnalysisTaskPrompt { get; set; }
}
