using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IUserRepository : IGenericeRepository<User>
    {
        Task<User?> GetByIdWithConnectionAsync(Guid userId, bool trackChanges = false);
    }
}
