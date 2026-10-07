using FabricExplorerBackend.Features.Fabric.Operation;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Workers.Processor
{
    /// <summary>
    /// Processor chạy bên trong BackgroundService (singleton) nên không inject trực tiếp các service scoped.
    /// Mỗi operation được xử lý trong một scope riêng (DbContext riêng) để:
    /// - chạy song song an toàn (DbContext không thread-safe),
    /// - lỗi lưu của operation này không ảnh hưởng đến operation khác.
    /// </summary>
    public class FabricOperationProcessor(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<FabricOperationProcessor> logger) : IFabricOperationProcessor
    {
        public async Task ProcessNotDoneOperationsAsync(CancellationToken cancellationToken = default)
        {
            var dueOperationIds = await GetDueOperationIdsAsync();
            if (dueOperationIds.Count == 0)
                return;

            logger.LogDebug("Polling {Count} fabric operation(s).", dueOperationIds.Count);

            var maxParallelism = configuration.GetValue("Fabric:OperationWorkerMaxParallelism", 4);
            using var gate = new SemaphoreSlim(maxParallelism);

            await Task.WhenAll(dueOperationIds.Select(async operationId =>
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var operationService = scope.ServiceProvider.GetRequiredService<IFabricOperationService>();
                    await operationService.RefreshAsync(operationId, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Ứng dụng đang tắt
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to refresh fabric operation {OperationId}.", operationId);
                }
                finally
                {
                    gate.Release();
                }
            }));
        }

        private async Task<IReadOnlyList<Guid>> GetDueOperationIdsAsync()
        {
            using var scope = scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var now = DateTimeOffset.UtcNow;
            var operations = await unitOfWork.FabricOperationRepository.GetAllNotDoneOperationsAsync();

            return operations
                .Where(o => o.NextPollAt is null || o.NextPollAt <= now)
                .Select(o => o.Id)
                .ToList();
        }
    }
}
