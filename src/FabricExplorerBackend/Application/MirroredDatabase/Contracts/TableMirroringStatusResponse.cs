using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Application.MirroredDatabase.Contracts
{
    public class TableMirroringStatusResponse
    {
        public string TableName { get; set; } = null!;
        public string Source { get; set; } = null!;
        public string Target { get; set; } = null!;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TableMirroringStatus Status { get; set; }
        public DateTimeOffset? LastSync { get; set; }
        public int? Lag { get; set; }
        public long ProcessedRows { get; set; }
    }
}
