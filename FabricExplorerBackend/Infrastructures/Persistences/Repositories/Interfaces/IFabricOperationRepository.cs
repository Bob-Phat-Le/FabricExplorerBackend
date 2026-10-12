using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IFabricOperationRepository : IGenericRepository<FabricOperation>
    {
        Task<List<FabricOperation>> GetAllNotDoneOperationsAsync(bool trackChanges = false, CancellationToken cancellationToken = default);
    }
}
