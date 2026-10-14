using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements
{
    public class FabricOperationRepository(FabricExplorerDbContext context) : IFabricOperationRepository
    {
        public async Task<IReadOnlyList<Guid>> GetDueOperationIdsAsync(DateTimeOffset now, int take, CancellationToken ct)
        => await context.Operations
            .AsNoTracking()
            .Where(o => (o.Status == FabricOperationStatus.Pending || o.Status == FabricOperationStatus.Running)
                        && o.NextPollAt <= now
                        && (o.LockedUntil == null || o.LockedUntil < now))
            .OrderBy(o => o.NextPollAt)
            .Select(o => o.Id)
            .Take(take)
            .ToListAsync(ct);

        public async Task<bool> TryClaimAsync(Guid id, DateTimeOffset now, TimeSpan lease, CancellationToken ct)
        {
            var lockedUntil = now + lease;

            var affected = await context.Operations
                .Where(o => o.Id == id
                            && (o.Status == FabricOperationStatus.Pending || o.Status == FabricOperationStatus.Running)
                            && (o.LockedUntil == null || o.LockedUntil < now))
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.LockedUntil, lockedUntil), ct);

            return affected == 1;
        }

        // ---- Các phương thức còn lại giữ nguyên như hiện tại ----

        public Task AddAsync(FabricOperation entity, CancellationToken cancellationToken = default)
            => context.Operations.AddAsync(entity, cancellationToken).AsTask();

        public Task<FabricOperation?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Operations.AsQueryable();
            if (!trackChanges)
                query = query.AsNoTracking();

            return query.FirstOrDefaultAsync(op => op.Id == id, cancellationToken);
        }

        public Task<(List<FabricOperation>, int)> GetAllAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public void Update(FabricOperation entity) => throw new NotImplementedException();
        public void Delete(FabricOperation entity) => throw new NotImplementedException();
        public Task DeleteAll() => throw new NotImplementedException();
    }
}
