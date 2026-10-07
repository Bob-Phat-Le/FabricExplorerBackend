using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Helpers
{
    public class FabricLroHelper(IConfiguration configuration) : IFabricLroHelper
    {
        public FabricOperationStatus MapStatus(string? fabricStatus) =>
            fabricStatus?.Trim().ToLowerInvariant() switch
            {
                "notstarted" => FabricOperationStatus.Pending,
                "running" => FabricOperationStatus.Running,
                "succeeded" => FabricOperationStatus.Succeeded,
                "failed" => FabricOperationStatus.Failed,
                "cancelled" or "canceled" => FabricOperationStatus.Cancelled,
                // Undefined hoặc giá trị lạ: coi như đang chạy và tiếp tục poll
                _ => FabricOperationStatus.Running
            };

        public bool IsDone(FabricOperationStatus status) =>
            status is FabricOperationStatus.Succeeded
                or FabricOperationStatus.Failed
                or FabricOperationStatus.Cancelled;

        public DateTimeOffset GetNextPollAt(DateTimeOffset now, TimeSpan? retryAfter = null)
        {
            var fallback = TimeSpan.FromSeconds(configuration.GetValue("Fabric:OperationPollIntervalSeconds", 5));
            var delay = retryAfter is { } value && value > TimeSpan.Zero ? value : fallback;
            return now + delay;
        }

        public bool IsExpired(DateTimeOffset createdAt, DateTimeOffset now) =>
            now - createdAt > TimeSpan.FromHours(configuration.GetValue("Fabric:OperationMaxAgeHours", 24));
    }
}
