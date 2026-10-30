using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Infrastructures.BackgroundJobs
{
    /// <summary>Execution mechanism only: periodically runs <see cref="IQueryResultCleanupService"/>.</summary>
    public sealed class QueryResultCleanupWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<QueryOptions> options,
        ILogger<QueryResultCleanupWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(options.Value.CleanupInterval);
            try
            {
                do
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var cleanup = scope.ServiceProvider.GetRequiredService<IQueryResultCleanupService>();
                        await cleanup.CleanupExpiredAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(ex, "Query result cleanup failed");
                    }
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
