using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Export;
using FabricExplorerBackend.Application.Configuration.Import;

namespace FabricExplorerBackend.Application.Configuration.Handlers
{
    /// <summary>
    /// Everything the configuration feature knows about one kind of entry. Adding a new kind
    /// (e.g. ingestion presets) means adding a handler and registering it; nothing else changes.
    /// </summary>
    public interface IConfigurationEntryHandler
    {
        string Type { get; }
        /// <summary>Name of the collection in the file.</summary>
        string Collection { get; }
        /// <summary>Dependency order: lower runs first on import and last on export.</summary>
        int Order { get; }
        Task<IReadOnlyList<ConfigurationItemResponse>> ListInventoryAsync(Guid userId, CancellationToken cancellationToken);
        Task CollectExportAsync(ExportContext context, CancellationToken cancellationToken);
        Task<IReadOnlyList<PreviewEntry>> PreviewAsync(PreviewContext context, CancellationToken cancellationToken);
        Task<IReadOnlyList<ImportUnit>> PlanImportAsync(ImportSession session, CancellationToken cancellationToken);
    }
}
