using FabricExplorerBackend.Domain.Enums;
using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase
{
    public class TableMirroringStatusResponse
    {
        public string TableName { get; set; } = null!;
        // schema.table ở nguồn
        public string Source { get; set; } = null!;
        // schema.table trong OneLake (Fabric)
        public string Target { get; set; } = null!;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TableMirroringStatus Status { get; set; }
        public DateTimeOffset? LastSync { get; set; }
        // Giây
        public int? Lag { get; set; }
        public long ProcessedRows { get; set; }
    }
}
