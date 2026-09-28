namespace FabricExplorerBackend.Models.Requests.Connection
{
    public class CreateConnectionRequest
    {
        public string Name { get; set; } = null!;
        public Guid TenantId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid ClientId { get; set; }
        public string ClientSecret { get; set; } = null!;
    }
}
