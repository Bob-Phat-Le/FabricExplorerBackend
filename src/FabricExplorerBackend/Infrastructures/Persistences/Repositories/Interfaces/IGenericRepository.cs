namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IGenericRepository<T>
    {
        Task<T?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);
        Task<(List<T>, int)> GetAllAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        void Update(T entity);
        void Delete(T entity);
    }
}
