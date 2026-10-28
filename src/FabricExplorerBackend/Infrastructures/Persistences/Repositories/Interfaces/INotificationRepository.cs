using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<(List<Notification>, int)> GetByUserIdAsync(Guid userId, int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default, bool unreadOnly = false);
        Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> MarkAllAsReadAsync(Guid userId, DateTimeOffset readAt, CancellationToken cancellationToken = default);
    }
}
