using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Entities.Abstractions;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements
{
    public class ConnectionRepository(FabricExplorerDbContext context) : IConnectionRepository
    {
        public async Task AddAsync(Connection entity)
        {
            await context.Connections.AddAsync(entity);
        }

        public async Task<Connection?> CheckDuplicateWorkspaceIdAsync(Guid workspaceId)
        {
            return await context.Connections.FirstOrDefaultAsync(c => !c.IsDeleted && c.WorkspaceId == workspaceId);
        }

        public async Task Delete(Connection entity)
        {
            if (entity is ISoftDeletable softDeletableEntity)
            {
                softDeletableEntity.IsDeleted = true;
                softDeletableEntity.DeletedAt = DateTimeOffset.UtcNow;
                context.Connections.Update(entity);
            }
            else
                context.Connections.Remove(entity);
        }

        public async Task DeleteAll()
        {
            throw new NotImplementedException();
        }

        public async Task<(IEnumerable<Connection>, int)> GetAllAsync(int skip, int take, bool trackChanges = false)
        {
            var query = context.Connections.AsQueryable().Where(c => !c.IsDeleted);
            var count = await query.CountAsync();
            if (!trackChanges)
                query = query.AsNoTracking();
            if (skip > 0)
                query = query.Skip(skip);
            if (take > 0)
                query = query.Take(take);
            var result = await query.ToListAsync();
            return (result, count);
        }

        public async Task<Connection?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Connections.AsQueryable().Where(c => !c.IsDeleted);
            if (!trackChanges)
                query = query.AsNoTracking();
            return await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task Update(Connection entity)
        {
            context.Connections.Update(entity);
        }
    }
}
