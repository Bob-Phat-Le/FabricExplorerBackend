using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements
{
    public class LandingZoneRepository(FabricExplorerDbContext context) : ILandingZoneRepository
    {
        public Task<LandingZoneConfig?> GetConfigAsync(Guid connectionId, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.LandingZoneConfigs.AsQueryable();
            if (!trackChanges)
                query = query.AsNoTracking();
            return query.FirstOrDefaultAsync(c => c.ConnectionId == connectionId, cancellationToken);
        }

        public Task AddConfigAsync(LandingZoneConfig config, CancellationToken cancellationToken = default)
            => context.LandingZoneConfigs.AddAsync(config, cancellationToken).AsTask();

        public void UpdateConfig(LandingZoneConfig config) => context.LandingZoneConfigs.Update(config);

        public void DeleteConfig(LandingZoneConfig config) => context.LandingZoneConfigs.Remove(config);

        public Task AddOperationsAsync(IEnumerable<LandingZoneOperation> operations, CancellationToken cancellationToken = default)
            => context.LandingZoneOperations.AddRangeAsync(operations, cancellationToken);

        public Task<List<LandingZoneOperation>> GetOperationsAsync(Guid connectionId, int take, CancellationToken cancellationToken = default)
            => context.LandingZoneOperations
                .AsNoTracking()
                .Where(o => o.ConnectionId == connectionId)
                .OrderByDescending(o => o.CreatedAt)
                .ThenByDescending(o => o.Id)
                .Take(take)
                .ToListAsync(cancellationToken);

        public async Task TrimOperationsAsync(Guid connectionId, int keep, CancellationToken cancellationToken = default)
        {
            // CreatedAt of the first operation that no longer fits in the retention window.
            var cutoff = await context.LandingZoneOperations
                .Where(o => o.ConnectionId == connectionId)
                .OrderByDescending(o => o.CreatedAt)
                .Skip(keep)
                .Select(o => (DateTimeOffset?)o.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (cutoff is null) return;

            await context.LandingZoneOperations
                .Where(o => o.ConnectionId == connectionId && o.CreatedAt <= cutoff)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
