namespace FabricExplorerBackend.Domain.Entities.Abstractions
{
    public interface ISoftDeletable
    {
        public bool IsDeleted => DeletedAt.HasValue;
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
