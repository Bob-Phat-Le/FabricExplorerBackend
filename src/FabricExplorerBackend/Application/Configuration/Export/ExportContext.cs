using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;

namespace FabricExplorerBackend.Application.Configuration.Export
{
    /// <summary>State shared by the handlers while one export runs.</summary>
    public sealed class ExportContext
    {
        public required Guid UserId { get; init; }
        public required bool ScopeAll { get; init; }
        public required ExportInclude Include { get; init; }
        public required IConfigurationDocumentBuilder Builder { get; init; }

        /// <summary>Type -> ids chosen by the user (scope "selected").</summary>
        public Dictionary<string, HashSet<Guid>> Selected { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Connections that exported saved queries point at; exported even if not chosen.</summary>
        public HashSet<Guid> RequiredConnectionIds { get; } = [];

        /// <summary>Saved queries left out because their connection is gone.</summary>
        public int Skipped { get; set; }

        public bool IsSelected(string type, Guid id)
            => ScopeAll || (Selected.TryGetValue(type, out var ids) && ids.Contains(id));
    }
}
