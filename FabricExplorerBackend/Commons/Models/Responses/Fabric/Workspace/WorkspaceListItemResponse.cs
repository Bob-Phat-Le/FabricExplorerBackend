namespace FabricExplorerBackend.Commons.Models.Responses.Workspace
{
    public class WorkspaceListItemResponse
    {
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = null!;
        public Guid? CapacityId { get; set; }
    }
}
