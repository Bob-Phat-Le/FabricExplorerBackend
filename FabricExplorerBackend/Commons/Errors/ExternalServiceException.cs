namespace FabricExplorerBackend.Commons.Errors
{
    public sealed class ExternalServiceException : Exception
    {
        public string? RequestId { get; set; }
        public string? ErrorCode { get; set; }
        public bool IsRetriable { get; set; }

        public ExternalServiceException(string? requestId, string? errorCode, string? message, bool isRetriable) : base(message)
        {
            RequestId = requestId;
            ErrorCode = errorCode;
            IsRetriable = isRetriable;
        }
    }
}
