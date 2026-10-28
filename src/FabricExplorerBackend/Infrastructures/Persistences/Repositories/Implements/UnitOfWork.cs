using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements
{
    public class UnitOfWork(
        IUserRepository userRepository,
        FabricExplorerDbContext context,
        IConnectionRepository connectionRepository,
        IFabricOperationRepository fabricOperationRepository,
        INotificationRepository notificationRepository) : IUnitOfWork
    {
        public async Task CommitTransaction(CancellationToken cancellationToken = default)
        {
            await context.Database.CommitTransactionAsync(cancellationToken);
        }

        public async Task RollbackTransaction(CancellationToken cancellationToken = default)
        {
            await context.Database.RollbackTransactionAsync(cancellationToken);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IDbContextTransaction> StartTransaction(CancellationToken cancellationToken = default)
        {
            return await context.Database.BeginTransactionAsync(cancellationToken);
        }

        public void Dispose()
        {
            context.Dispose();
            GC.SuppressFinalize(this);
        }

        public IConnectionRepository ConnectionRepository => connectionRepository;
        public IUserRepository UserRepository => userRepository;
        public IFabricOperationRepository FabricOperationRepository => fabricOperationRepository;
        public INotificationRepository NotificationRepository => notificationRepository;
    }
}
