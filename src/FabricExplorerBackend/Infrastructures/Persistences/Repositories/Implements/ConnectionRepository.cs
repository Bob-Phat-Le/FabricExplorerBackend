using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Entities.Abstractions;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements
{
    public class ConnectionRepository(FabricExplorerDbContext context) : IConnectionRepository
    {
        public Task AddAsync(Connection entity, CancellationToken cancellationToken = default)
        {
            return context.Connections.AddAsync(entity, cancellationToken).AsTask();
        }

        public Task<Connection?> CheckDuplicateWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return context.Connections.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).FirstOrDefaultAsync(c => c.DeletedAt == null && c.WorkspaceId == workspaceId, cancellationToken);
        }

        public Task<Connection?> FindByExternalKeyAsync(Guid externalKey, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Connections.Where(c => c.DeletedAt == null && c.ExternalKey == externalKey);
            if (!trackChanges)
                query = query.AsNoTracking();
            return query.FirstOrDefaultAsync(cancellationToken);
        }

        public Task<Connection?> FindByWorkspaceIdAsync(Guid workspaceId, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Connections.Where(c => c.DeletedAt == null && c.WorkspaceId == workspaceId);
            if (!trackChanges)
                query = query.AsNoTracking();
            return query.FirstOrDefaultAsync(cancellationToken);
        }

        public Task<List<Connection>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Connections.Where(c => c.DeletedAt == null && ids.Contains(c.Id));
            if (!trackChanges)
                query = query.AsNoTracking();
            return query.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).ToListAsync(cancellationToken);
        }

        public void Delete(Connection entity)
        {
            if (entity is ISoftDeletable softDeletableEntity)
            {
                softDeletableEntity.DeletedAt = DateTimeOffset.UtcNow;
                context.Connections.Update(entity);
            }
            else
                context.Connections.Remove(entity);
        }

        public async Task<(List<Connection>, int)> GetAllAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Connections.Where(c => c.DeletedAt == null).OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).AsQueryable();
            var count = await query.CountAsync(cancellationToken);
            if (!trackChanges)
                query = query.AsNoTracking();
            if (skip > 0)
                query = query.Skip(skip);
            if (take > 0)
                query = query.Take(take);
            var result = await query.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).ToListAsync(cancellationToken);
            return (result, count);
        }

        public async Task<Connection?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Connections.AsQueryable().Where(c => c.DeletedAt == null);
            if (!trackChanges)
                query = query.AsNoTracking();
            return await query.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public void Update(Connection entity)
        {
            context.Connections.Update(entity);
        }
    }
}
