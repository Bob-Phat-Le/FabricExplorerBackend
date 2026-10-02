using FabricExplorerBackend.Entities;
using FabricExplorerBackend.Entities.Abstractions;
using FabricExplorerBackend.Persistences;
using FabricExplorerBackend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Repositories.Implements
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
                entity.IsDeleted = true;
                entity.DeletedAt = DateTimeOffset.UtcNow;
                context.Connections.Update(entity);
            }
            else
                context.Connections.Remove(entity);
        }

        public async Task DeleteAll()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Connection>> GetAllAsync(bool trackChanges = false)
        {
            var query = context.Connections.AsQueryable();
            if (!trackChanges)
                query = query.AsNoTracking();
            return await query.Where(c => !c.IsDeleted).ToListAsync();
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
