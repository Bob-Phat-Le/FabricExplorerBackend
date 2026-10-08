using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IConnectionRepository : IGenericeRepository<Connection>
    {
        Task<Connection?> CheckDuplicateWorkspaceIdAsync(Guid workspaceId);
    }
}
