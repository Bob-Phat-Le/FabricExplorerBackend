using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Helpers
{
    public interface IFabricLroHelper
    {
        /// <summary>Đổi chuỗi status của Fabric sang FabricOperationStatus của backend.</summary>
        FabricOperationStatus MapStatus(string? fabricStatus);

        /// <summary>Operation đã kết thúc (thành công, thất bại hoặc bị hủy) thì không cần hỏi Fabric nữa.</summary>
        bool IsDone(FabricOperationStatus status);

        /// <summary>Thời điểm sớm nhất được hỏi lại Fabric. Ưu tiên Retry-After của Fabric, nếu không có thì dùng cấu hình.</summary>
        DateTimeOffset GetNextPollAt(DateTimeOffset now, TimeSpan? retryAfter = null);

        /// <summary>Operation chạy quá lâu so với giới hạn cấu hình thì coi như thất bại để không poll mãi.</summary>
        bool IsExpired(DateTimeOffset createdAt, DateTimeOffset now);
    }
}
