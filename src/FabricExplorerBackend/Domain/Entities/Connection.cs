using FabricExplorerBackend.Domain.Entities.Abstractions;

namespace FabricExplorerBackend.Domain.Entities
{
    public class Connection : BaseEntity, IAuditable, ISoftDeletable
    {
        // Base properties
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public bool IsDeleted => DeletedAt.HasValue;
        public DateTimeOffset? DeletedAt { get; set; }

        // Connection properties
        public string Name { get; set; } = null!;

        /// <summary>Stable identifier used by import/export to match connections between environments.</summary>
        // we need to make sure that the ExternalKey is unique across all connections, so we should add a unique constraint on this column in the database
        // we need this property because the same connection not necessary has the same Id in different environments, so we need a stable identifier to match them; the ExternalKey is generated when the connection is created, and it should never change; if the connection is deleted and recreated, it should have a new ExternalKey
        public Guid ExternalKey { get; set; }
        public const int EnvironmentMaxLength = 64;
        public string? Environment { get; set; }
        public Guid TenantId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid ClientId { get; set; }
        public string ClientSecret { get; set; } = null!;

        /// <summary>Derived: an imported connection has no secret until the user enters one.</summary>
        public string CredentialStatus => string.IsNullOrEmpty(ClientSecret) ? CredentialStatuses.Missing : CredentialStatuses.Configured;

        // References to related entities
        public IEnumerable<User> Users { get; set; } = null!;
    }
}

namespace FabricExplorerBackend.Domain.Entities
{
    public static class CredentialStatuses
    {
        public const string Configured = "Configured";
        public const string Missing = "Missing";
    }
}
