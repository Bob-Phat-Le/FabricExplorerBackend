namespace FabricExplorerBackend.Commons.Models.Requests.Fabric.FabricOperation
{
    public class CreateFabricOperationRequest
    {
        public Guid OperationId { get; set; }
        public Uri OperationUrl { get; set; } = null!;
        public TimeSpan RetryAfter { get; set; }

        public Guid ConnectionId { get; set; }
        public string OperationType { get; set; } = null!;
        public string? ResourceId { get; set; }
    }
}