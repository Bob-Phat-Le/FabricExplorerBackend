using FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Formats;
using FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Generation;
using FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Typing;
using FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Xsd;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate
{
    public static class MetadataGenerationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the metadata generator. A new file format, type rule or record discovery strategy
        /// is one more line here and no change in the existing classes.
        /// </summary>
        public static IServiceCollection AddMetadataGeneration(this IServiceCollection services)
        {
            // file formats
            services.AddSingleton<IMetadataFormat, CsvMetadataFormat>();
            services.AddSingleton<IMetadataFormat, DelimitedTextMetadataFormat>();
            services.AddSingleton<IMetadataFormat, ParquetMetadataFormat>();
            services.AddSingleton<IMetadataFormatRegistry, MetadataFormatRegistry>();
            services.AddSingleton<IMetadataGenerator, MetadataGenerator>();

            // XSD type -> Fabric type chain (order = priority)
            services.AddSingleton<IFabricTypeRule, BoundedIntegerRule>();
            services.AddSingleton<IFabricTypeRule, WholeNumberDecimalRule>();
            services.AddSingleton<IFabricTypeRule, BuiltInTypeRule>();
            services.AddSingleton<IFabricTypeResolver, FabricTypeResolver>();

            // XSD analysis (order = priority)
            services.AddSingleton<IRecordDiscoveryStrategy, RepeatingChildRecords>();
            services.AddSingleton<IRecordDiscoveryStrategy, GlobalElementRecord>();
            services.AddSingleton<XsdSchemaLoader>();
            services.AddSingleton<XsdFieldExtractor>();
            services.AddSingleton<IXsdAnalyzer, XsdAnalyzer>();

            return services;
        }
    }
}
