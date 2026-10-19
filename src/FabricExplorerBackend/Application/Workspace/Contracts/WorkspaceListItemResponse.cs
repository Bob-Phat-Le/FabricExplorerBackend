namespace FabricExplorerBackend.Application.Workspace.Contracts
{
    public class WorkspaceListItemResponse
    {
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = null!;
        public Guid? CapacityId { get; set; }
    }
}
