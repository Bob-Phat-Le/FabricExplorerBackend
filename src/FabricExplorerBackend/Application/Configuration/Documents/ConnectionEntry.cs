namespace FabricExplorerBackend.Application.Configuration.Documents
{
    public sealed class ConnectionEntry
    {
        public string? Key { get; set; }
        public string? Name { get; set; }
        public string? Environment { get; set; }
        public string? WorkspaceId { get; set; }
        public string? WorkspaceName { get; set; }
        public string? TenantId { get; set; }
        public string? TokenEndpoint { get; set; }
        public string? ClientId { get; set; }
        public ConnectionCredentials? Credentials { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
