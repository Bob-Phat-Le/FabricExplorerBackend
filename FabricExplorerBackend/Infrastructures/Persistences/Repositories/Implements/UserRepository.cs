using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Entities.Abstractions;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements
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

        public async Task<(IEnumerable<User>, int)> GetAllAsync(int skip, int take, bool trackChanges = false)
        {
            var query = context.Users.AsQueryable();
            var count = query.Count();
            if (!trackChanges)
                query.AsNoTracking();
            if (skip > 0)
                query = query.Skip(skip);
            if (take > 0)
                query = query.Take(take);
            var result = await query.Where(u => !u.IsDeleted).ToListAsync();
            return (result, count);
        }

        public async Task<User?> GetByIdAsync(Guid id, bool trackChanges = false)
        {
            var query = context.Users.AsQueryable();
            if (!trackChanges)
                query.AsNoTracking();
            return await query.FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == id);
        }

        public async Task<User?> GetByIdWithConnectionAsync(Guid userId, bool trackChanges = false)
        {
            var query = context.Users.AsQueryable().Include(u => u.ActiveConnection);
            if (!trackChanges)
                query.AsNoTracking();
            return await query.FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == userId);
        }

        public async Task Update(User entity)
        {
            context.Users.Update(entity);
        }
    }
}
