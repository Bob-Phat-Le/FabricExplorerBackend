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
                        // Lỗi cả vòng (ví dụ DB tạm thời không kết nối được): log rồi đợi tick sau.
                        // KHÔNG để lọt ra ngoài: exception thoát khỏi ExecuteAsync làm host dừng cả ứng dụng.
                        logger.LogError(ex, "Fabric operation polling tick failed");
                    }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // Ứng dụng đang tắt, thoát êm.
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

            // Tuần tự cho đơn giản (mỗi lần poll chỉ là một HTTP GET ngắn). Muốn song song thì dùng
            // Parallel.ForEachAsync: vì mỗi operation đã có DI scope riêng nên không đụng DbContext của nhau.
            foreach (var id in dueIds)
            {
                ct.ThrowIfCancellationRequested();

                // DI scope (và DbContext) riêng cho từng operation: một operation lỗi lưu DB
                // không làm hỏng trạng thái change tracker của các operation phía sau.
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IFabricOperationProcessor>();

                try
                {
                    await processor.PollAsync(id, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Processor đã tự xử lý các lỗi poll dự đoán được. Vào đây là lỗi hiếm (ví dụ SaveChanges thất bại).
                    // Dòng này đã bị claim nên sẽ được thử lại sau khi lease hết hạn, không tạo vòng lặp nóng.
                    logger.LogError(ex, "Polling operation {OperationId} crashed", id);
                }
            }
        }
    }
}
