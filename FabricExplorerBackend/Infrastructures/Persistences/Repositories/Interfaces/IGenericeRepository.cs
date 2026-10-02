namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IGenericeRepository<T>
    {
        Task<T?> GetByIdAsync(Guid id, bool trackChanges = false);
        Task<IEnumerable<T>> GetAllAsync(bool trackChanges = false);
        Task AddAsync(T entity);
        Task Update(T entity);
        Task Delete(T entity);
        Task DeleteAll();
    }
}
