using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.Configuration.Export;
using FabricExplorerBackend.Application.Configuration.Handlers;
using FabricExplorerBackend.Application.Configuration.Import;
using FabricExplorerBackend.Application.Configuration.Validation;
using FluentValidation;

namespace FabricExplorerBackend.Application.Configuration
{
    public static class ConfigurationServiceCollectionExtensions
    {
        public static IServiceCollection AddConfigurationTransfer(this IServiceCollection services)
        {
            // services
            services.AddScoped<IConfigurationCatalogService, ConfigurationCatalogService>();
            services.AddScoped<IConfigurationExportService, ConfigurationExportService>();
            services.AddScoped<IConfigurationImportService, ConfigurationImportService>();
            services.AddScoped<IConfigurationHistoryService, ConfigurationHistoryService>();

            // document
            services.AddSingleton<IIntegrityHasher, Sha256IntegrityHasher>();
            services.AddScoped<IConfigurationDocumentReader, ConfigurationDocumentReader>();
            services.AddTransient<IConfigurationDocumentBuilder, ConfigurationDocumentBuilder>();

            // one handler per entry type
            services.AddScoped<IConfigurationEntryHandler, ConnectionConfigurationHandler>();
            services.AddScoped<IConfigurationEntryHandler, SavedQueryConfigurationHandler>();
            services.AddScoped<IConfigurationHandlerRegistry, ConfigurationHandlerRegistry>();
            services.AddScoped<IConnectionImporter, ConnectionImporter>();
            services.AddScoped<ISavedQueryImporter, SavedQueryImporter>();
            services.AddScoped<IConnectionReferenceBuilder, ConnectionReferenceBuilder>();

            // extension points of a connection
            services.AddScoped<IConnectionExportContributor, LandingZoneExportContributor>();
            services.AddScoped<IConnectionImportStep, LandingZoneImportStep>();

            // import strategies
            services.AddScoped<IImportModeStrategy, MergeImportModeStrategy>();
            services.AddScoped<IImportModeStrategy, OverwriteImportModeStrategy>();
            services.AddScoped<IImportExecutionStrategy, BestEffortExecutionStrategy>();
            services.AddScoped<IImportExecutionStrategy, AtomicExecutionStrategy>();

            // validators (FluentValidation)
            services.AddScoped<IValidator<ConnectionEntry>, ConnectionEntryValidator>();
            services.AddScoped<IValidator<SavedQueryEntry>, SavedQueryEntryValidator>();
            services.AddScoped<IValidator<ExportRequest>, ExportRequestValidator>();
            services.AddScoped<IValidator<ImportRequest>, ImportRequestValidator>();
            services.AddScoped<IValidator<ImportIdempotencyKey>, ImportIdempotencyKeyValidator>();
            services.AddScoped<IValidator<ConfigurationFileHeader>, ConfigurationFileHeaderValidator>();
            services.AddScoped<IValidator<ConfigurationDocument>, ConfigurationDocumentValidator>();

            return services;
        }
    }
}
