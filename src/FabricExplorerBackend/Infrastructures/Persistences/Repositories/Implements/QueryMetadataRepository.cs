using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements
{
    public class QueryMetadataRepository(FabricExplorerDbContext context) : IQueryMetadataRepository
    {
        public Task AddAsync(QueryMetadata entity, CancellationToken cancellationToken = default)
            => context.Queries.AddAsync(entity, cancellationToken).AsTask();

        public void Update(QueryMetadata entity) => context.Queries.Update(entity);

        public void Delete(QueryMetadata entity) => context.Queries.Remove(entity);

        public Task<QueryMetadata?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Queries.AsQueryable();
            if (!trackChanges)
                query = query.AsNoTracking();

            return query.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        }

        public async Task<(List<QueryMetadata>, int)> GetAllAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
            => await PageAsync(context.Queries.AsQueryable(), skip, take, trackChanges, cancellationToken);

        public async Task<(List<QueryMetadata>, int)> GetByUserIdAsync(Guid userId, int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
            => await PageAsync(context.Queries.Where(q => q.UserId == userId), skip, take, trackChanges, cancellationToken);

        public async Task<bool> TryMarkRunningAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            var affected = await context.Queries
                .Where(q => q.Id == id && q.Status == QueryStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.Status, QueryStatus.Running)
                    .SetProperty(q => q.UpdatedAt, (DateTimeOffset?)now), cancellationToken);

            return affected == 1;
        }

        public async Task<bool> TryMarkCancelledAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            var affected = await context.Queries
                .Where(q => q.Id == id && q.Status == QueryStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.Status, QueryStatus.Cancelled)
                    .SetProperty(q => q.CompletedAt, (DateTimeOffset?)now)
                    .SetProperty(q => q.UpdatedAt, (DateTimeOffset?)now), cancellationToken);

            return affected == 1;
        }

        public async Task<IReadOnlyList<Guid>> FailInterruptedAsync(DateTimeOffset now, string error, CancellationToken cancellationToken = default)
        {
            var ids = await context.Queries
                .AsNoTracking()
                .Where(q => q.Status == QueryStatus.Pending || q.Status == QueryStatus.Running)
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            if (ids.Count == 0)
                return ids;

            await context.Queries
                .Where(q => ids.Contains(q.Id) && (q.Status == QueryStatus.Pending || q.Status == QueryStatus.Running))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.Status, QueryStatus.Failed)
                    .SetProperty(q => q.Error, error)
                    .SetProperty(q => q.CompletedAt, (DateTimeOffset?)now)
                    .SetProperty(q => q.UpdatedAt, (DateTimeOffset?)now), cancellationToken);

            return ids;
        }

        public async Task<IReadOnlyList<Guid>> GetExpiredIdsAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default)
            => await context.Queries
                .AsNoTracking()
                .Where(q => q.ExpiresAt <= now
                            && (q.Status == QueryStatus.Completed
                                || q.Status == QueryStatus.Failed
                                || q.Status == QueryStatus.Cancelled))
                .OrderBy(q => q.ExpiresAt)
                .Select(q => q.Id)
                .Take(take)
                .ToListAsync(cancellationToken);

        public Task<int> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => context.Queries.Where(q => q.Id == id).ExecuteDeleteAsync(cancellationToken);

        private static async Task<(List<QueryMetadata>, int)> PageAsync(
            IQueryable<QueryMetadata> source, int skip, int take, bool trackChanges, CancellationToken cancellationToken)
        {
            var ordered = source.OrderByDescending(q => q.CreatedAt).ThenBy(q => q.Id);
            var count = await ordered.CountAsync(cancellationToken);

            IQueryable<QueryMetadata> page = ordered;
            if (!trackChanges)
                page = page.AsNoTracking();
            if (skip > 0)
                page = page.Skip(skip);
            if (take > 0)
                page = page.Take(take);

            return (await page.ToListAsync(cancellationToken), count);
        }
    }
}
