using FabricExplorerBackend.Application.QueryService;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Infrastructures.BackgroundJobs
{
    /// <summary>Runs the queued queries with at most <see cref="QueryOptions.MaxConcurrentQueries"/> in parallel.</summary>
    public sealed class QueryExecutionService(
        IQueryJobQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<QueryOptions> options,
        TimeProvider clock,
        ILogger<QueryExecutionService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RecoverInterruptedQueriesAsync(stoppingToken);

            var workers = Enumerable
                .Range(0, Math.Max(1, options.Value.MaxConcurrentQueries))
                .Select(_ => RunWorkerAsync(stoppingToken))
                .ToArray();

            try
            {
                await Task.WhenAll(workers);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task RunWorkerAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var job in queue.DequeueAsync(stoppingToken))
                {
                    using var scope = scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<IQueryProcessor>();

                    try
                    {
                        await processor.RunAsync(job, stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(ex, "Running query {QueryId} crashed", job.QueryId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        // The queue lives in memory: after a restart, Pending/Running rows have nobody working on them.
        private async Task RecoverInterruptedQueriesAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var store = scope.ServiceProvider.GetRequiredService<IQueryResultStore>();

                var ids = await unitOfWork.QueryMetadataRepository.FailInterruptedAsync(
                    clock.GetUtcNow(), "The query was interrupted by a server restart.", cancellationToken);

                foreach (var id in ids)
                    await store.DeleteAsync(id, cancellationToken);

                if (ids.Count > 0)
                    logger.LogWarning("Marked {Count} interrupted query(ies) as failed", ids.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Cannot recover interrupted queries");
            }
        }
    }
}
