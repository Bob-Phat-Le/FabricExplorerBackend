using FabricExplorerBackend.Entities.Abstractions;

namespace FabricExplorerBackend.Entities
{
    public class UserPreference : BaseEntity, IAuditable, ISoftDeletable
    {

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public Guid CreatedBy { get; set; }

        public Guid? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid LastUsedConnectionId { get; set; }
        public Connection LastUsedConnection { get; set; } = null!;
    }
}
