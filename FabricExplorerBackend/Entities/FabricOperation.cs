using FabricExplorerBackend.Entities.Abstractions;
using FabricExplorerBackend.Enums;
using System.ComponentModel.DataAnnotations;

namespace FabricExplorerBackend.Entities
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
        //public string? ResourceId { get; set; }
        //public string? ErrorCode { get; set; }
        //public string? ErrorMessage { get; set; }
        //public string? ResultJson { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
