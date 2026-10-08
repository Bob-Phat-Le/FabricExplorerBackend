namespace FabricExplorerBackend.Commons.Models.Requests.FabricOperation
{
    public class CreateFabricOperationRequest
    {
        // Lấy từ header của response 202 (x-ms-operation-id, Location, Retry-After)
        public Guid OperationId { get; set; }
        public Uri OperationUrl { get; set; } = null!;
        public TimeSpan RetryAfter { get; set; }

        // Ngữ cảnh do nơi gọi cung cấp để worker có thể poll và biết operation thuộc về item nào
        public string OperationType { get; set; } = null!;
        public Guid ConnectionId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid? ResourceId { get; set; }
    }
}
