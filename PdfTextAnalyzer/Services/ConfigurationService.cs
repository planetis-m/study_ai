using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PdfTextAnalyzer.Configuration;

namespace PdfTextAnalyzer.Services;

public static class ConfigurationService
{
    public static void RegisterConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Register individual configuration sections (for services that need specific sections)
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));
        services.Configure<PipelineSettings>(configuration.GetSection(PipelineSettings.SectionName));
        services.Configure<ArchiveSettings>(configuration.GetSection(ArchiveSettings.SectionName));
        services.Configure<PdfExtractionSettings>(configuration.GetSection(PdfExtractionSettings.SectionName));
        services.Configure<PreprocessingSettings>(configuration.GetSection(PreprocessingSettings.SectionName));
        services.Configure<AnalysisSettings>(configuration.GetSection(AnalysisSettings.SectionName));

        // Register the unified ApplicationSettings as a singleton
        services.AddSingleton(provider =>
        {
            var appSettings = new ApplicationSettings();
            configuration.Bind(appSettings);
            return appSettings;
        });
    }
}
