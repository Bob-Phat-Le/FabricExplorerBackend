using Microsoft.EntityFrameworkCore.Storage;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<IDbContextTransaction> StartTransaction();
        Task CommitTransaction();
        Task RollbackTransaction();
        Task<int> SaveChangesAsync();

        IConnectionRepository ConnectionRepository { get; }
        IUserRepository UserRepository { get; }
        IFabricOperationRepository FabricOperationRepository { get; }
    }
}
