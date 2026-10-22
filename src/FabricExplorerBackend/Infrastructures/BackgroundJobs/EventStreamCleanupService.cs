using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Infrastructures.BackgroundJobs
{
    /// <summary>Định kỳ gỡ các stream đã hết hạn khỏi registry để không rò bộ nhớ.</summary>
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
