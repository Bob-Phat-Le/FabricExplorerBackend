namespace FabricExplorerBackend.Entities.Abstractions
{
    public interface IAuditable
    {
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset? UpdatedAt { get; }
        public Guid CreatedBy { get; }
        public Guid? UpdatedBy { get; }
    }
}
