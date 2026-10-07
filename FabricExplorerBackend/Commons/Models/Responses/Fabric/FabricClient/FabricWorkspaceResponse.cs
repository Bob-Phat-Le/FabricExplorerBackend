namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient
{
    // Map response của GET /workspaces và GET /workspaces/{workspaceId}
    public class FabricWorkspaceResponse
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Type { get; set; }
        public Guid? CapacityId { get; set; }
    }
}
