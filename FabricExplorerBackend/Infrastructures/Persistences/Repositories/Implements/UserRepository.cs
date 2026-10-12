using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Entities.Abstractions;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements
{
    public class UserRepository(FabricExplorerDbContext context) : IUserRepository
    {
        public Task AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            return context.Users.AddAsync(entity, cancellationToken).AsTask();
        }

        public void Delete(User entity)
        {
            if (entity is ISoftDeletable softDeletableEntity)
            {
                entity.DeletedAt = DateTimeOffset.UtcNow;
                context.Users.Update(entity);
            }
            else
                context.Users.Remove(entity);
        }

        public async Task<(List<User>, int)> GetAllAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Users.AsQueryable().Where(u => u.DeletedAt == null);
            var count = await query.CountAsync(cancellationToken);
            if (!trackChanges)
                query.AsNoTracking();
            if (skip > 0)
                query = query.Skip(skip);
            if (take > 0)
                query = query.Take(take);
            var result = await query.ToListAsync(cancellationToken);
            return (result, count);
        }

        public Task<User?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Users.AsQueryable().Where(u => u.DeletedAt == null);
            if (!trackChanges)
                query.AsNoTracking();
            return query.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public Task<User?> GetByIdWithConnectionAsync(Guid userId, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Users.AsQueryable().Where(u => u.DeletedAt == null).Include(u => u.ActiveConnection);
            if (!trackChanges)
                query.AsNoTracking();
            return query.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        public void Update(User entity)
        {
            context.Users.Update(entity);
        }
    }
}
