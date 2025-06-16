using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public static class ConfigurationService
{
    public static void RegisterConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Register individual configuration sections (for services that need specific sections)
        services.AddOptions<AiSettings>()
            .Bind(configuration.GetSection(AiSettings.SectionName))
            .ValidateDataAnnotations();
        services.AddOptions<PipelineSettings>()
            .Bind(configuration.GetSection(PipelineSettings.SectionName))
            .ValidateDataAnnotations();
        services.AddOptions<ArchiveSettings>()
            .Bind(configuration.GetSection(ArchiveSettings.SectionName))
            .ValidateDataAnnotations();
        services.AddOptions<PdfExtractionSettings>()
            .Bind(configuration.GetSection(PdfExtractionSettings.SectionName))
            .ValidateDataAnnotations();
        services.AddOptions<PreprocessingSettings>()
            .Bind(configuration.GetSection(PreprocessingSettings.SectionName))
            .ValidateDataAnnotations();
        services.AddOptions<AnalysisSettings>()
            .Bind(configuration.GetSection(AnalysisSettings.SectionName))
            .ValidateDataAnnotations();

        // Register the unified ApplicationSettings as a singleton
        services.AddSingleton(provider =>
        {
            var appSettings = new ApplicationSettings();
            configuration.Bind(appSettings);
            return appSettings;
        });
    }
}
