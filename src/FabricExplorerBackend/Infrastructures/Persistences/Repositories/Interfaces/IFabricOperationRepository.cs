using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IFabricOperationRepository : IGenericRepository<FabricOperation>
    {
        Task<IReadOnlyList<Guid>> GetDueOperationIdsAsync(DateTimeOffset now, int take, CancellationToken ct);
        Task<bool> TryClaimAsync(Guid id, DateTimeOffset now, TimeSpan lease, CancellationToken ct);
    }
}
