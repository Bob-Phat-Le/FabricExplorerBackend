using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public sealed record SavedQueryFilter(string? Search = null, Guid? ConnectionId = null, string? ItemType = null);

    /// <summary>Saved queries are private to their owner, so every read is scoped by owner.</summary>
    public interface ISavedQueryRepository
    {
        Task<SavedQuery?> GetOwnedAsync(Guid ownerId, Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);
        Task<(List<SavedQuery>, int)> SearchAsync(Guid ownerId, SavedQueryFilter filter, int skip, int take, CancellationToken cancellationToken = default);
        Task<List<SavedQuery>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
        Task<SavedQuery?> FindByKeyAsync(Guid ownerId, Guid externalKey, bool trackChanges = false, CancellationToken cancellationToken = default);
        Task<SavedQuery?> FindByNameAsync(Guid ownerId, Guid connectionId, string name, bool trackChanges = false, CancellationToken cancellationToken = default);

        Task AddAsync(SavedQuery entity, CancellationToken cancellationToken = default);
        void Update(SavedQuery entity);
        void Delete(SavedQuery entity);
    }
}
