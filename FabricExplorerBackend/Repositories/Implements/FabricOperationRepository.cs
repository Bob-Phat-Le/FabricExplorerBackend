using FabricExplorerBackend.Entities;
using FabricExplorerBackend.Persistences;
using FabricExplorerBackend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Repositories.Implements
{
    public class FabricOperationRepository(FabricExplorerDbContext context) : IFabricOperationRepository
    {
        public async Task AddAsync(FabricOperation entity)
        {
            await context.Operations.AddAsync(entity);
        }

        public Task Delete(FabricOperation entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAll()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FabricOperation>> GetAllAsync(bool trackChanges = false)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<FabricOperation>> GetAllNotDoneOperationsAsync(bool trackChanges = false)
        {
            var query = context.Operations.AsQueryable();
            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.Where(op => op.Status == Enums.FabricOperationStatus.Pending || op.Status == Enums.FabricOperationStatus.Running).ToListAsync();
        }

        public Task<FabricOperation?> GetByIdAsync(Guid id, bool trackChanges = false)
        {
            var query = context.Operations.AsQueryable();
            if (!trackChanges)
                query = query.AsNoTracking();

            return query.FirstOrDefaultAsync(op => op.Id == id);
        }

        public Task Update(FabricOperation entity)
        {
            throw new NotImplementedException();
        }
    }
}
