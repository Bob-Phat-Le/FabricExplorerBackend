using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements
{
    public class SavedQueryRepository(FabricExplorerDbContext context) : ISavedQueryRepository
    {
        public Task<SavedQuery?> GetOwnedAsync(Guid ownerId, Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.SavedQueries.Where(q => q.OwnerId == ownerId && q.Id == id);
            if (!trackChanges)
                query = query.AsNoTracking();
            return query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<(List<SavedQuery>, int)> SearchAsync(
            Guid ownerId, SavedQueryFilter filter, int skip, int take, CancellationToken cancellationToken = default)
        {
            var query = context.SavedQueries.AsNoTracking().Where(q => q.OwnerId == ownerId);

            if (filter.ConnectionId is { } connectionId)
                query = query.Where(q => q.ConnectionId == connectionId);

            if (!string.IsNullOrWhiteSpace(filter.ItemType))
            {
                var itemType = filter.ItemType.Trim().ToLower();
                query = query.Where(q => q.ItemType.ToLower() == itemType);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(q => q.Name.ToLower().Contains(term) || q.ItemName.ToLower().Contains(term));
            }

            var count = await query.CountAsync(cancellationToken);

            var page = query.OrderByDescending(q => q.UpdatedAt ?? q.CreatedAt).ThenBy(q => q.Name).ThenBy(q => q.Id);
            var items = await (take > 0 ? page.Skip(skip).Take(take) : page).ToListAsync(cancellationToken);
            return (items, count);
        }

        public Task<List<SavedQuery>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
            => context.SavedQueries
                .AsNoTracking()
                .Where(q => q.OwnerId == ownerId)
                .OrderBy(q => q.CreatedAt).ThenBy(q => q.Id)
                .ToListAsync(cancellationToken);

        public Task<SavedQuery?> FindByKeyAsync(Guid ownerId, Guid externalKey, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.SavedQueries.Where(q => q.OwnerId == ownerId && q.ExternalKey == externalKey);
            if (!trackChanges)
                query = query.AsNoTracking();
            return query.FirstOrDefaultAsync(cancellationToken);
        }

        public Task<SavedQuery?> FindByNameAsync(Guid ownerId, Guid connectionId, string name, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.SavedQueries.Where(q => q.OwnerId == ownerId && q.ConnectionId == connectionId && q.Name == name);
            if (!trackChanges)
                query = query.AsNoTracking();
            return query.FirstOrDefaultAsync(cancellationToken);
        }

        public Task AddAsync(SavedQuery entity, CancellationToken cancellationToken = default)
            => context.SavedQueries.AddAsync(entity, cancellationToken).AsTask();

        public void Update(SavedQuery entity) => context.SavedQueries.Update(entity);

        public void Delete(SavedQuery entity) => context.SavedQueries.Remove(entity);
    }
}
