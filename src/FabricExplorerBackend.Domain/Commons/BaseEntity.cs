namespace FabricExplorerBackend.Domain.Entities.Abstractions
{
    public class BaseEntity : IEntity
    {
        public Guid Id { get; set; }
    }
}
