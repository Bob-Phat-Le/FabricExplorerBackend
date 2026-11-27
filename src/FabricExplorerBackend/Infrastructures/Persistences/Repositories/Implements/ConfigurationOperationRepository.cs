using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements
{
    public class ConfigurationOperationRepository(FabricExplorerDbContext context) : IConfigurationOperationRepository
    {
        public Task AddAsync(ConfigurationOperation operation, CancellationToken cancellationToken = default)
            => context.ConfigurationOperations.AddAsync(operation, cancellationToken).AsTask();

        public Task<ConfigurationOperation?> FindByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken = default)
            => context.ConfigurationOperations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.UserId == userId && o.IdempotencyKey == idempotencyKey, cancellationToken);

        public Task<List<ConfigurationOperation>> GetRecentAsync(Guid userId, int take, CancellationToken cancellationToken = default)
            => context.ConfigurationOperations
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ThenByDescending(o => o.Id)
                .Take(take)
                .ToListAsync(cancellationToken);

        public async Task TrimAsync(Guid userId, int keep, CancellationToken cancellationToken = default)
        {
            var cutoff = await context.ConfigurationOperations
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Skip(keep)
                .Select(o => (DateTimeOffset?)o.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (cutoff is null) return;

            await context.ConfigurationOperations
                .Where(o => o.UserId == userId && o.CreatedAt <= cutoff)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
