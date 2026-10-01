using FabricExplorerBackend.Entities;

namespace FabricExplorerBackend.Repositories.Interfaces
{
    public interface IUserRepository : IGenericeRepository<User>
    {
        Task<User?> GetByIdWithConnectionAsync(Guid userId, bool trackChanges = false);
    }
}
