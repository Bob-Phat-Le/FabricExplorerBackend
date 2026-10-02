namespace FabricExplorerBackend.Commons.Models.Requests.FabricOperation
{
    public class CreateFabricOperationRequest
    {
        public Guid OperationId { get; set; }
        public Uri OperationUrl { get; set; } = null!;
        public TimeSpan RetryAfter { get; set; }
    }
}