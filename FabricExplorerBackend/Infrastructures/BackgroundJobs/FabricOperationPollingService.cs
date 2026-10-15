using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Workers.Processor;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Infrastructures.BackgroundJobs
{
    public sealed class FabricOperationPollingService(
        IServiceScopeFactory scopeFactory,
        IOptions<OperationPollingOptions> options,
        TimeProvider clock,
        ILogger<FabricOperationPollingService> logger) : BackgroundService
    {
        private readonly OperationPollingOptions opt = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!opt.Enabled)
            {
                logger.LogInformation("Fabric operation polling is disabled");
                return;
            }

            using var timer = new PeriodicTimer(opt.TickInterval);
            try
            {
                do
                    try
                    {
                        await RunTickAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(ex, "Fabric operation polling tick failed");
                    }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task RunTickAsync(CancellationToken ct)
        {
            IReadOnlyList<Guid> dueIds;
            using (var queryScope = scopeFactory.CreateScope())
            {
                var repo = queryScope.ServiceProvider.GetRequiredService<IUnitOfWork>().FabricOperationRepository;
                dueIds = await repo.GetDueOperationIdsAsync(clock.GetUtcNow(), opt.BatchSize, ct);
            }

            foreach (var id in dueIds)
            {
                ct.ThrowIfCancellationRequested();

                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IFabricOperationProcessor>();

                try
                {
                    await processor.PollAsync(id, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Polling operation {OperationId} crashed", id);
                }
            }
        }
    }
}
