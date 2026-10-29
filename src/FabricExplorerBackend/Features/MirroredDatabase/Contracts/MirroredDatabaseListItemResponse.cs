namespace FabricExplorerBackend.Features.MirroredDatabase.Contracts
{
    public class MirroredDatabaseListItemResponse
    {
        public Guid MirroredDatabaseId { get; set; }
        public string MirroredDatabaseName { get; set; } = null!;
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = null!;
        public Guid ConnectionId { get; set; }
    }
}
