using System.Text.Json;

namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed class ImportRequest
    {
        public JsonElement Document { get; set; }
        /// <summary>"merge" (default) or "overwrite".</summary>
        public string? Mode { get; set; }
        /// <summary>null or empty = every entry of the file.</summary>
        public List<ImportEntryRef>? Selection { get; set; }
        public ImportOptions? Options { get; set; }
    }
}
