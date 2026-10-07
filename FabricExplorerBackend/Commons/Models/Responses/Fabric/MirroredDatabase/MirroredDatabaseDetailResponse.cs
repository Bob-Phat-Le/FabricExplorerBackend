using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase
{
    public class MirroredDatabaseDetailResponse
    {
        public Guid MirroredDatabaseId { get; set; }
        public string MirroredDatabaseName { get; set; } = null!;
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = null!;
        public string Source { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
        public MirroringStatus Status { get; set; }
    }
}
