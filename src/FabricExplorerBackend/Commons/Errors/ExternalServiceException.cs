namespace FabricExplorerBackend.Commons.Errors
{
    public sealed class ExternalServiceException(
        string service, string operation, int? upstreamStatus, string? upstreamCode,
        string? upstreamRequestId, string? upstreamMessage, string? responseBody, Exception? inner = null)
        : Exception($"{service} '{operation}' failed: HTTP {upstreamStatus?.ToString() ?? "n/a"} {upstreamCode} {upstreamMessage}".Trim(), inner)
    {
        public string Service { get; } = service;
        public string Operation { get; } = operation;
        public int? UpstreamStatus { get; } = upstreamStatus;
        public string? UpstreamCode { get; } = upstreamCode;
        public string? UpstreamRequestId { get; } = upstreamRequestId;
        public string? ResponseBody { get; } = responseBody;
    }
}
