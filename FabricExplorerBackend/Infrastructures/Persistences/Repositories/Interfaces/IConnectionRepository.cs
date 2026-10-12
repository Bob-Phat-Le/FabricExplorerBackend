using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IConnectionRepository : IGenericRepository<Connection>
    {
        Task<Connection?> CheckDuplicateWorkspaceIdAsync(Guid workspaceId);
    }
}
