namespace FabricExplorerBackend.Domain.Entities.Abstractions
{
    public interface ISoftDeletable
    {
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
