namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation
{
    public class FabricOperationResponse
    {
        public Guid Id { get; set; }
        public string OperationType { get; set; } = null!;
        public string? ResourceId { get; set; }
        public string Status { get; set; } = null!;
        public int? PercentComplete { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? LastPolledAt { get; set; }
        public DateTimeOffset? NextPollAt { get; set; }
    }
}
