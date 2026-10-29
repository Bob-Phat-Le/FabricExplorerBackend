using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Persistences;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements
{
    public class NotificationRepository(FabricExplorerDbContext context) : INotificationRepository
    {
        public Task AddAsync(Notification entity, CancellationToken cancellationToken = default)
        {
            return context.Notifications.AddAsync(entity, cancellationToken).AsTask();
        }

        public void Delete(Notification entity)
        {
            context.Notifications.Remove(entity);
        }

        public async Task<(List<Notification>, int)> GetAllAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Notifications.OrderByDescending(noti => noti.CreatedAt).ThenBy(noti => noti.Id).AsQueryable();
            var count = await query.CountAsync(cancellationToken);
            if (!trackChanges)
                query = query.AsNoTracking();
            if (skip > 0)
                query = query.Skip(skip);
            if (take > 0)
                query = query.Take(take);
            var result = await query.ToListAsync(cancellationToken);
            return (result, count);
        }

        public async Task<(List<Notification>, int)> GetByUserIdAsync(Guid userId, int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default, bool unreadOnly = false)
        {
            var query = context.Notifications
                .Where(noti => noti.UserId == userId && (!unreadOnly || noti.ReadAt == null))
                .OrderByDescending(noti => noti.CreatedAt)
                .ThenBy(noti => noti.Id)
                .AsQueryable();

            var count = await query.CountAsync(cancellationToken);
            if (!trackChanges)
                query = query.AsNoTracking();
            if (skip > 0)
                query = query.Skip(skip);
            if (take > 0)
                query = query.Take(take);
            var result = await query.ToListAsync(cancellationToken);
            return (result, count);
        }

        public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return context.Notifications.CountAsync(noti => noti.UserId == userId && noti.ReadAt == null, cancellationToken);
        }

        public Task<int> MarkAllAsReadAsync(Guid userId, DateTimeOffset readAt, CancellationToken cancellationToken = default)
        {
            return context.Notifications
                .Where(noti => noti.UserId == userId && noti.ReadAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(noti => noti.ReadAt, (DateTimeOffset?)readAt), cancellationToken);
        }

        public Task<Notification?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Notifications.AsQueryable();
            if (!trackChanges)
                query = query.AsNoTracking();
            return query.FirstOrDefaultAsync(noti => noti.Id == id, cancellationToken);
        }

        public void Update(Notification entity)
        {
            context.Notifications.Update(entity);
        }
    }
}
