using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IFabricOperationRepository : IGenericeRepository<FabricOperation>
    {
        Task<IEnumerable<FabricOperation>> GetAllNotDoneOperationsAsync(bool trackChanges = false);
    }
}
