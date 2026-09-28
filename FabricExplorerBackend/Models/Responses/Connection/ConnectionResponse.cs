namespace FabricExplorerBackend.Models.Responses.Connection
{
    public class ConnectionResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid WorkspaceId { get; set; }
        public Guid TenantId { get; set; }
        public Guid ClientId { get; set; }
        public string ClientSecret { get; set; } = null!;
    }
}
