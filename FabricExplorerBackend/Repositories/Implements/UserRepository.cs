using FabricExplorerBackend.Entities;
using FabricExplorerBackend.Entities.Abstractions;
using FabricExplorerBackend.Persistences;
using FabricExplorerBackend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Repositories.Implements
{
    public class UserRepository(FabricExplorerDbContext context) : IUserRepository
    {
        public async Task AddAsync(User entity)
        {
            await context.Users.AddAsync(entity);
        }

        public async Task Delete(User entity)
        {
            if (entity is ISoftDeletable softDeletableEntity)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTimeOffset.UtcNow;
                context.Users.Update(entity);
            }
            else
                context.Users.Remove(entity);
        }

        public Task DeleteAll()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<User>> GetAllAsync(bool trackChanges = false)
        {
            var query = context.Users.AsQueryable();
            if (!trackChanges)
                query.AsNoTracking();
            return await query.Where(u => !u.IsDeleted).ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id, bool trackChanges = false)
        {
            var query = context.Users.AsQueryable();
            if (!trackChanges)
                query.AsNoTracking();
            return await query.FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == id);
        }

        public async Task Update(User entity)
        {
            context.Users.Update(entity);
        }
    }
}
