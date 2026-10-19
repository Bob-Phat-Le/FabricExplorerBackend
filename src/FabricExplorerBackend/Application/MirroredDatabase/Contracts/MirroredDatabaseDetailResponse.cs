using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Application.MirroredDatabase.Contracts
{
    public class MirroredDatabaseDetailResponse
    {
        public Guid MirroredDatabaseId { get; set; }
        public string MirroredDatabaseName { get; set; } = null!;
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = null!;

        public string? SourceType { get; set; }
        public string? SourceName { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MirroredDatabaseOnlineStatus Status { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MirroringStatus MirroringStatus { get; set; }
    }
}
