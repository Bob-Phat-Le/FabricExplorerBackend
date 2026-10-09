namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.Exceptions
{
    public sealed class FabricErrorResponse : Exception
    {
        public string? RequestId { get; set; }
        public string? ErrorCode { get; set; }
        public bool IsRetriable { get; set; }

        public FabricErrorResponse(string? requestId, string? errorCode, string? message, bool isRetriable) : base(message)
        {
            RequestId = requestId;
            ErrorCode = errorCode;
            IsRetriable = isRetriable;
        }
    }
}
