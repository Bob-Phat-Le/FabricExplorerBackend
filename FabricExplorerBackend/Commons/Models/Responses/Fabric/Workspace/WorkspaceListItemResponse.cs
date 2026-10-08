namespace FabricExplorerBackend.Commons.Models.Responses.Workspace
{
    /// <summary>Một workspace mà connection (service principal) nhìn thấy trong tenant.</summary>
    public class WorkspaceListItemResponse
    {
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = null!;
        public Guid? CapacityId { get; set; }
    }
}
