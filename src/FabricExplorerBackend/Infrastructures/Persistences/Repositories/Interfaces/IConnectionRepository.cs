using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IConnectionRepository : IGenericRepository<Connection>
    {
        Task<Connection?> CheckDuplicateWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default);
        Task<Connection?> FindByExternalKeyAsync(Guid externalKey, bool trackChanges = false, CancellationToken cancellationToken = default);
        Task<Connection?> FindByWorkspaceIdAsync(Guid workspaceId, bool trackChanges = false, CancellationToken cancellationToken = default);
        Task<List<Connection>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, bool trackChanges = false, CancellationToken cancellationToken = default);
    }
}
