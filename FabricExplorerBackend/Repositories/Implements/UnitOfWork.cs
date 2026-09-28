using FabricExplorerBackend.Persistences;
using FabricExplorerBackend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FabricExplorerBackend.Repositories.Implements
{
    public class UnitOfWork(
        FabricExplorerDbContext context,
        IConnectionRepository connectionRepository,
        IUserRepository userRepository) : IUnitOfWork
    {

        public async Task CommitTransaction()
        {
            await context.Database.CommitTransactionAsync();
        }

        public async Task RollbackTransaction()
        {
            await context.Database.RollbackTransactionAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> StartTransaction()
        {
            return await context.Database.BeginTransactionAsync();
        }
        public void Dispose()
        {
            context.Dispose();
            GC.SuppressFinalize(this);
        }

        public IConnectionRepository ConnectionRepository => connectionRepository;
        public IUserRepository UserRepository => userRepository;
    }
}
