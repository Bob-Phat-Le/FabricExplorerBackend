using System.Text.Json;

namespace FabricExplorerBackend.Application.Configuration.Documents
{
    public sealed record IntegrityPayload(
        IReadOnlyList<ConnectionEntry> Connections,
        IReadOnlyList<SavedQueryEntry> SavedQueries,
        JsonElement? Preferences);
}
