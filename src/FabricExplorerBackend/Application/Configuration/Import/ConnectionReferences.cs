using FabricExplorerBackend.Application.Configuration.Validation;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    /// <summary>Connections a saved query may point at, by the key used in the file.</summary>
    public sealed class ConnectionReferences
    {
        /// <summary>Normalised file key -> id of an existing connection.</summary>
        public Dictionary<string, Guid> Known { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Normalised keys of connections that are in the file and not in the system.</summary>
        public HashSet<string> Pending { get; } = new(StringComparer.OrdinalIgnoreCase);

        public ReferenceState Resolve(string? key, out Guid connectionId)
        {
            connectionId = Guid.Empty;

            if (!ConfigurationGuid.TryParse(key, out var parsed))
                return ReferenceState.Unresolved;

            var normalized = parsed.ToString("D");
            if (Known.TryGetValue(normalized, out connectionId))
                return ReferenceState.Existing;

            return Pending.Contains(normalized) ? ReferenceState.Pending : ReferenceState.Unresolved;
        }
    }
}
