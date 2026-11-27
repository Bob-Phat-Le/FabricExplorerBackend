namespace FabricExplorerBackend.Application.Connection.Contracts
{
    public class ConnectionResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid WorkspaceId { get; set; }
        public Guid TenantId { get; set; }
        public Guid ClientId { get; set; }
        public string ClientSecret { get; set; } = null!;
        public Guid ExternalKey { get; set; }
        public string? Environment { get; set; }
        /// <summary>"Configured" or "Missing" (an imported connection has no secret until one is entered).</summary>
        public string CredentialStatus { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
