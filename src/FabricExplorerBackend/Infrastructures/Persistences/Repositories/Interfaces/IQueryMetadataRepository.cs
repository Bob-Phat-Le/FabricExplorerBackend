using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface IQueryMetadataRepository : IGenericRepository<QueryMetadata>
    {
        Task<(List<QueryMetadata>, int)> GetByUserIdAsync(Guid userId, int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default);

        /// <summary>Atomically moves Pending -> Running. False when the query is gone or was cancelled while waiting.</summary>
        Task<bool> TryMarkRunningAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken = default);

        /// <summary>Atomically moves Pending -> Cancelled. False when the query already started (or finished).</summary>
        Task<bool> TryMarkCancelledAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken = default);

        /// <summary>Marks every Pending/Running query as Failed (used on start-up: their in-memory work is gone).</summary>
        Task<IReadOnlyList<Guid>> FailInterruptedAsync(DateTimeOffset now, string error, CancellationToken cancellationToken = default);

        /// <summary>Ids of finished queries whose retention has elapsed.</summary>
        Task<IReadOnlyList<Guid>> GetExpiredIdsAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default);

        Task<int> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
