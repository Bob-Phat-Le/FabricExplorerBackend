using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Configurations.Options;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Infrastructures.BackgroundJobs
{
    public sealed class EventStreamCleanupService(
        IEventRegistry registry,
        IOptions<EventStreamingOptions> options,
        ILogger<EventStreamCleanupService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(options.Value.CleanupInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        registry.RemoveExpired();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Event stream cleanup failed");
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
