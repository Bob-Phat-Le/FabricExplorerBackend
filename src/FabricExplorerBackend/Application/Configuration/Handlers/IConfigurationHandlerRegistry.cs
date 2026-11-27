namespace FabricExplorerBackend.Application.Configuration.Handlers
{
    public interface IConfigurationHandlerRegistry
    {
        /// <summary>Import order.</summary>
        IReadOnlyList<IConfigurationEntryHandler> Ascending { get; }

        /// <summary>Export order: saved queries are collected first so they can pull in their connections.</summary>
        IReadOnlyList<IConfigurationEntryHandler> Descending { get; }

        bool TryGet(string? type, out IConfigurationEntryHandler handler);
    }
}
