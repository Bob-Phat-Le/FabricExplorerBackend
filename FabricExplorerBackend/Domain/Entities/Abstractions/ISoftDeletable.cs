namespace FabricExplorerBackend.Domain.Entities.Abstractions
{
    public interface ISoftDeletable
    {
        public bool IsDeleted { get; }
        public DateTimeOffset? DeletedAt { get; }
    }
}
