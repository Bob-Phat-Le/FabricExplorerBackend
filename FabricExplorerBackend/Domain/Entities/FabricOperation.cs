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
        public Guid ConnectionId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid? ResourceId { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? ResourceName { get; set; }
        public int ConsecutivePollFailures { get; set; }
        public DateTimeOffset? LockedUntil { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
