namespace FabricExplorerBackend.Commons.Models.Responses.Fabric
{
    public class FabricContext
    {
        public Guid TenantId { get; set; }
        public Guid ConnectionId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Microsoft.Fabric.Api.FabricClient Client { get; set; } = null!;
    }
}
