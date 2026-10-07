using FabricExplorerBackend.Workers.Processor;

namespace FabricExplorerBackend.Workers
{
    /// <summary>
    /// Định kỳ gọi processor để backend chủ động cập nhật trạng thái các Fabric long running operation.
    /// Mốc NextPollAt của từng operation quyết định operation nào thực sự được hỏi lại Fabric trong mỗi lượt.
    /// </summary>
    public class FabricOperationWorker(
        IFabricOperationProcessor processor,
        IConfiguration configuration,
        ILogger<FabricOperationWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(configuration.GetValue("Fabric:OperationWorkerIntervalSeconds", 5));
            logger.LogInformation("Fabric operation worker started (interval: {Interval}).", interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await processor.ProcessNotDoneOperationsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Lỗi cả lượt (ví dụ DB tạm thời không truy cập được): ghi log và thử lại ở lượt sau
                    logger.LogError(ex, "Fabric operation worker iteration failed.");
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            logger.LogInformation("Fabric operation worker stopped.");
        }
    }
}
