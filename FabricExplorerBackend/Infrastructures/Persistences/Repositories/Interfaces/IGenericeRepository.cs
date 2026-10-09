namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IGenericeRepository<T>
    {
        Task<T?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);
        Task<(IEnumerable<T>, int)> GetAllAsync(int skip, int take, bool trackChanges = false);
        Task AddAsync(T entity);
        Task Update(T entity);
        Task Delete(T entity);
        Task DeleteAll();
    }
}
