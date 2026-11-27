using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IConfigurationOperationRepository
    {
        Task AddAsync(ConfigurationOperation operation, CancellationToken cancellationToken = default);
        Task<ConfigurationOperation?> FindByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken = default);
        Task<List<ConfigurationOperation>> GetRecentAsync(Guid userId, int take, CancellationToken cancellationToken = default);

        /// <summary>Keeps only the newest <paramref name="keep"/> operations of the user.</summary>
        Task TrimAsync(Guid userId, int keep, CancellationToken cancellationToken = default);
    }
}
