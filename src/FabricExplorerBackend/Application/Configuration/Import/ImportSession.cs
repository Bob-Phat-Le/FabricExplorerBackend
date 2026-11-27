using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.Configuration.Validation;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    /// <summary>Everything an import needs while it runs. Handlers read it, importers record created ids in it.</summary>
    public sealed class ImportSession
    {
        public required Guid UserId { get; init; }
        public required ConfigurationDocument Document { get; init; }
        public required ImportOptions Options { get; init; }
        public required IImportModeStrategy Mode { get; init; }
        public required ConnectionReferences References { get; init; }

        /// <summary>"Type|key" of the chosen entries; null = everything in the file.</summary>
        public IReadOnlySet<string>? Selection { get; init; }

        public bool IsSelected(string type, string? key)
        {
            if (Selection is null)
                return true;

            return ConfigurationGuid.TryParse(key, out var parsed)
                && Selection.Contains($"{type}|{parsed:D}");
        }
    }
}
