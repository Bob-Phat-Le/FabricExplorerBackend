namespace FabricExplorerBackend.Commons.Models.Responses.FabricOperation
{
    public class OperationInformationResponse
    {
        public Guid OperationId { get; set; }
        public Uri OperationUrl { get; set; } = null!;
        public TimeSpan RetryAfter { get; set; }
    }
}
