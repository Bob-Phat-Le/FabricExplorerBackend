using FabricExplorerBackend.Domain.Entities.Abstractions;
using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Domain.Entities
{
    public class FabricOperation : BaseEntity
    {
        public Guid FabricOperationId { get; set; }
        public string? FabricOperationUrl { get; set; }
        public string OperationType { get; set; } = default!;
        public FabricOperationStatus Status { get; set; }
        public DateTimeOffset? NextPollAt { get; set; }
        public DateTimeOffset? LastPolledAt { get; set; }
        public int? PercentComplete { get; set; }

        // Connection đã khởi tạo operation: cần để lấy token khi hỏi lại trạng thái từ Fabric
        public Guid ConnectionId { get; set; }
        // Đối tượng bị tác động, ví dụ "{lakehouseId}/{tableName}" với LoadTable
        public string? ResourceId { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        //public string? ResultJson { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
