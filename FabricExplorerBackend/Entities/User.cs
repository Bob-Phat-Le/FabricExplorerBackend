using FabricExplorerBackend.Entities.Abstractions;

namespace FabricExplorerBackend.Entities
{
    public class User : BaseEntity, IAuditable, ISoftDeletable
    {

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public Guid CreatedBy { get; set; }

        public Guid? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }

        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public Guid ActiveConnectionId { get; set; }
        public Connection ActiveConnection { get; set; } = null!;
    }
}
