using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements
{
    public class FabricOperationRepository(FabricExplorerDbContext context) : IFabricOperationRepository
    {
        public Task AddAsync(FabricOperation entity, CancellationToken cancellationToken = default)
        {
            return context.Operations.AddAsync(entity, cancellationToken).AsTask();
        }

        public void Delete(FabricOperation entity)
        {
            throw new NotImplementedException();
        }

        public Task<(List<FabricOperation>, int)> GetAllAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<FabricOperation>> GetAllNotDoneOperationsAsync(bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Operations.AsQueryable();
            if (!trackChanges)
                query = query.AsNoTracking();

            return query.Where(op => op.Status == FabricOperationStatus.Pending || op.Status == FabricOperationStatus.Running).OrderBy(c => c.CreatedAt).ToListAsync(cancellationToken);
        }

        public Task<FabricOperation?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
        {
            var query = context.Operations.AsQueryable();
            if (!trackChanges)
                query = query.AsNoTracking();

            return query.OrderBy(c => c.CreatedAt).FirstOrDefaultAsync(op => op.Id == id, cancellationToken);
        }

        public void Update(FabricOperation entity)
        {
            throw new NotImplementedException();
        }
    }
}
