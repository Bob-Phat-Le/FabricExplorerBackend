namespace FabricExplorerBackend.Application.Operation.Contracts
{
    public class CreateFabricOperationRequest
    {
        public Guid OperationId { get; set; }
        public Uri OperationUrl { get; set; } = null!;
        public TimeSpan RetryAfter { get; set; }

        public string OperationType { get; set; } = null!;
        public Guid ConnectionId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid? ResourceId { get; set; }
        public string ResourceName { get; set; } = null!;
    }
}
