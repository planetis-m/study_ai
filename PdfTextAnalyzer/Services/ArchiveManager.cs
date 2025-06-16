using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;
using PdfTextAnalyzer.Models;
using PdfTextAnalyzer.Validation;

namespace PdfTextAnalyzer.Services;

public class ArchiveManager : IArchiveManager
{
    private readonly ArchiveSettings _settings;
    private readonly ApplicationSettings _appSettings;

    public ArchiveManager(
        ApplicationSettings appSettings,
        IOptions<ArchiveSettings> settings)
    {
        _appSettings = Guard.NotNull(appSettings, nameof(appSettings));
        _settings = Guard.NotNullOptions(settings, nameof(settings));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    public async Task ArchiveResultAsync(PipelineResult result, CancellationToken cancellationToken)
    {
        Guard.NotNull(result, nameof(result));

        try
        {
            // Generate unique run identifier and timestamp
            var runId = Guid.NewGuid();
            var executionTimestamp = DateTime.UtcNow;
            var timestampString = executionTimestamp.ToString("yyyyMMddHHmmssfff");

            // Create sanitized directory name for the PDF
            var pdfDirectoryName = CreateSanitizedDirectoryName(result.PdfPath);
            var pdfArchiveDirectory = Path.Combine(_settings.BaseArchiveDirectory, pdfDirectoryName);

            cancellationToken.ThrowIfCancellationRequested();

            // Ensure the archive directory exists
            Directory.CreateDirectory(pdfArchiveDirectory);

            // Create the archive filename
            var archiveFileName = $"{timestampString}_{runId:N}_PipelineResult.json";
            var archiveFilePath = Path.Combine(pdfArchiveDirectory, archiveFileName);

            // Create minimal archive configuration with only essential model info
            var archiveConfig = CreateArchiveConfiguration(_appSettings);

            // Create the archive object
            var archiveData = new PipelineArchive
            {
                RunMetadata = new RunMetadata
                {
                    RunId = runId,
                    ExecutionTimestampUtc = executionTimestamp,
                    Configuration = archiveConfig
                },
                PipelineData = result
            };

            cancellationToken.ThrowIfCancellationRequested();

            // Serialize and write to file
            var jsonContent = JsonSerializer.Serialize(archiveData, JsonOptions);
            await File.WriteAllTextAsync(archiveFilePath, jsonContent, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to archive pipeline result: {ex.Message}", ex);
        }
    }

    private static ArchiveConfiguration CreateArchiveConfiguration(ApplicationSettings settings)
    {
        var config = new ArchiveConfiguration();

        // Only include model info for stages that are enabled
        if (settings.Pipeline.Preprocessing)
        {
            config.Preprocessing = settings.Preprocessing;
        }

        if (settings.Pipeline.Analysis)
        {
            config.Analysis = settings.Analysis;
        }

        // Create hash of the minimal configuration for integrity checking
        config.ConfigurationHash = CreateConfigurationHash(settings);

        return config;
    }

    private static string CreateConfigurationHash(ApplicationSettings settings)
    {
        // Serialize to JSON for consistent hashing
        var configJson = JsonSerializer.Serialize(settings, JsonOptions);

        // Create SHA256 hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(configJson));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string CreateSanitizedDirectoryName(string pdfPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(pdfPath);

        if (string.IsNullOrWhiteSpace(fileName))
            return CreateHashDirectoryName(pdfPath);

        // URL encoding approach - creates safe strings
        var sanitized = Uri.EscapeDataString(fileName);

        if (sanitized.Length > 100)
            return CreateHashDirectoryName(pdfPath);

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
    public ArchiveConfiguration Configuration { get; set; } = new();
}

public class ArchiveConfiguration
{
    public string ConfigurationHash { get; set; } = string.Empty;

    // Only include model info for active stages
    public ModelSettings? Preprocessing { get; set; }
    public ModelSettings? Analysis { get; set; }
}
