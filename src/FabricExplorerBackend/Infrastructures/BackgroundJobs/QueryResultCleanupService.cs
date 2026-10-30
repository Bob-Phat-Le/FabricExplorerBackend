using FabricExplorerBackend.Application.QueryService;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Infrastructures.BackgroundJobs
{
    public sealed class QueryResultCleanupService(
        IUnitOfWork unitOfWork,
        IQueryResultStore store,
        IOptions<QueryOptions> options,
        TimeProvider clock,
        ILogger<QueryResultCleanupService> logger) : IQueryResultCleanupService
    {
        public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
        {
            var repository = unitOfWork.QueryMetadataRepository;
            var batchSize = Math.Max(1, options.Value.CleanupBatchSize);
            var removed = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var ids = await repository.GetExpiredIdsAsync(clock.GetUtcNow(), batchSize, cancellationToken);
                if (ids.Count == 0)
                    break;

                var anyFailed = false;
                foreach (var id in ids)
                {
                    try
                    {
                        // Files first: if the row delete fails the row stays expired and is retried next tick.
                        await store.DeleteAsync(id, cancellationToken);
                        await repository.DeleteByIdAsync(id, cancellationToken);
                        removed++;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        anyFailed = true;
                        logger.LogWarning(ex, "Cannot remove expired query {QueryId}", id);
                    }
                }

                // A failing id would be returned again by the next batch, so do not spin on it.
                if (anyFailed || ids.Count < batchSize)
                    break;
            }

            if (removed > 0)
                logger.LogInformation("Removed {Count} expired query result(s)", removed);

            return removed;
        }
    }
}
