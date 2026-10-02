using FabricExplorerBackend.Entities;

namespace FabricExplorerBackend.Repositories.Interfaces
{
    public interface IFabricOperationRepository : IGenericeRepository<FabricOperation> 
    {
        Task<IEnumerable<FabricOperation>> GetAllNotDoneOperationsAsync(bool trackChanges = false);
    }
}
