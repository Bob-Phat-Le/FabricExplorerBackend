using FabricExplorerBackend.Domain.Enums;
using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase
{
    public class MirroredDatabaseDetailResponse
    {
        public Guid MirroredDatabaseId { get; set; }
        public string MirroredDatabaseName { get; set; } = null!;
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = null!;

        // Loại nguồn (AzureSqlDatabase, Snowflake, ...) và tên nguồn (tên database nguồn) lấy từ mirroring.json
        public string? SourceType { get; set; }
        public string? SourceName { get; set; }

        // Fabric API không trả ngày tạo cho mirrored database nên có thể null
        public DateTimeOffset? CreatedAt { get; set; }

        // Active / Offline cho giao diện
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MirroredDatabaseOnlineStatus Status { get; set; }

        // Trạng thái mirroring gốc của Fabric (Running, Stopped, ...)
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MirroringStatus MirroringStatus { get; set; }
    }
}
