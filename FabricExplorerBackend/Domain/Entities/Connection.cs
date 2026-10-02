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
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        // Connection properties
        public string Name { get; set; } = null!;
        public Guid TenantId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid ClientId { get; set; }
        public string ClientSecret { get; set; } = null!;

        // References to related entities
        public IEnumerable<User> Users { get; set; } = null!;
    }
}
