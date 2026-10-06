using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Entities.Abstractions;
using FabricExplorerBackend.Infrastructures.Persistences;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructure.Repositories
{
    public class ConnectionRepository(FabricExplorerDbContext context) : IConnectionRepository
    {
        public async Task AddAsync(Connection entity)
        {
            await context.Connections.AddAsync(entity);
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
            var query = context.Connections.AsQueryable();
            var count = query.Count();
            if (!trackChanges)
                query = query.AsNoTracking();
            if (skip > 0)
                query = query.Skip(skip);
            if (take > 0)
                query = query.Take(take);
            var result = await query.Where(c => !c.IsDeleted).ToListAsync();
            return (result, count);
        }

        public async Task<Connection?> GetByIdAsync(Guid id, bool trackChanges = false)
        {
            var query = context.Connections.AsQueryable();
            if (!trackChanges)
                query = query.AsNoTracking();
            return await query.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task Update(Connection entity)
        {
            context.Connections.Update(entity);
        }
    }
}
