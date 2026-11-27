using System.Text.Json;

namespace FabricExplorerBackend.Application.Configuration.Documents
{
    /// <summary>
    /// The file written by export and read by import. Property order is the order in the file
    /// and also the order used to compute the integrity hash, so do not reorder.
    /// </summary>
    public sealed class ConfigurationDocument
    {
        public string? Format { get; set; }
        public int FormatVersion { get; set; }
        public DateTimeOffset? ExportedAt { get; set; }
        public ConfigurationAppInfo? App { get; set; }
        public string? Scope { get; set; }
        public Dictionary<string, int>? Contents { get; set; }

        public List<ConnectionEntry>? Connections { get; set; }
        public List<SavedQueryEntry>? SavedQueries { get; set; }

        /// <summary>Reserved for application settings once they are persisted.</summary>
        public JsonElement? Preferences { get; set; }

        public ConfigurationIntegrity? Integrity { get; set; }
    }
}
