using Microsoft.Fabric.Api;

namespace FabricExplorerBackend.Commons.Models.Responses
{
    public class FabricContext
    {
        public Guid TenantId { get; set; }
        public Guid ConnectionId { get; set; }
        public Guid WorkspaceId { get; set; }
        public FabricClient Client { get; set; } = null!;
    }
}
