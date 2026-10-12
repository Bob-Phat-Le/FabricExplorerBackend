using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Application.MirroredDatabase.Contracts
{
    public class MirroringDatabaseStatusResponse
    {
        public DateTimeOffset? LastSynchronization { get; set; }
        public long RecordSynchronized { get; set; }
        // Giây; null khi chưa có bảng nào đồng bộ
        public int? CurrentLatency { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MirroringStatus Status { get; set; }
    }
}
