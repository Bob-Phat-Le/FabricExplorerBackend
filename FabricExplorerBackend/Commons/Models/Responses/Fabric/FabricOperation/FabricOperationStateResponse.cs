namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation
{
    public class FabricOperationStateResponse
    {
        public string? Status { get; set; }
        public DateTimeOffset? CreatedTimeUtc { get; set; }
        public DateTimeOffset? LastUpdatedTimeUtc { get; set; }
        public int? PercentComplete { get; set; }
        public FabricOperationErrorResponse? Error { get; set; }
    }

    public class FabricOperationErrorResponse
    {
        public string? ErrorCode { get; set; }
        public string? Message { get; set; }
    }
}
