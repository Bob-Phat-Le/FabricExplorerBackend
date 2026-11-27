using Microsoft.EntityFrameworkCore.Storage;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<IDbContextTransaction> StartTransaction(CancellationToken cancellationToken = default);
        Task CommitTransaction(CancellationToken cancellationToken = default);
        Task RollbackTransaction(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        IConnectionRepository ConnectionRepository { get; }
        IUserRepository UserRepository { get; }
        IFabricOperationRepository FabricOperationRepository { get; }
        INotificationRepository NotificationRepository { get; }
        ILandingZoneRepository LandingZoneRepository { get; }
        ISavedQueryRepository SavedQueryRepository { get; }
        IConfigurationOperationRepository ConfigurationOperationRepository { get; }

        /// <summary>Detaches every tracked entity, e.g. after a failed SaveChanges so later work starts clean.</summary>
        void ClearTracking();
    }
}
