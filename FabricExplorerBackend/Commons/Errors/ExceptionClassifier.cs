using Azure;
using Azure.Identity;
using FabricExplorerBackend.Commons.Results;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;

namespace FabricExplorerBackend.Commons.Errors
{
    public sealed record Failure(int Status, string Code, string Message, LogLevel Level, bool LogStackTrace, object? Details = null);

    public static class ExceptionClassifier
    {
        public static Failure Classify(Exception ex) => ex switch
        {
            ApplicationException e => new(e.Error.Kind.ToStatusCode(), e.Error.Code, e.Error.Message, LogLevel.Information, false),

            AuthenticationFailedException e => new(400, ErrorCodes.Auth.TokenFailed,
                "Failed to create access token, check tenant id, client id, and client secret",
                LogLevel.Warning, false, new { upstream = e.Message }),

            ExternalServiceException e => FromUpstream(e.UpstreamStatus, e.UpstreamCode, e.UpstreamRequestId, e.Message),
            RequestFailedException e => FromUpstream(e.Status, e.ErrorCode, e.GetRawResponse()?.Headers.RequestId, e.Message),

            SqlException e => new(502, ErrorCodes.Sql.QueryFailed, "Failed to query Sql endpoint", LogLevel.Error, true, new { e.Number }),
            TimeoutException or TaskCanceledException => new(504, ErrorCodes.Upstream.Timeout,
                "External service reached timeout", LogLevel.Warning, false),
            RedisException => new(503, ErrorCodes.Cache.Unavailable, "Cache is unavailable", LogLevel.Error, true),

            _ => new(500, ErrorCodes.Internal.Unexpected, "Unexpected error occured", LogLevel.Error, true)
        };

        private static Failure FromUpstream(int? status, string? code, string? requestId, string message)
        {
            var details = new { upstreamStatus = status, upstreamCode = code, upstreamRequestId = requestId };
            return status switch
            {
                404 => new(404, ErrorCodes.Fabric.NotFound, "Couldn't find Fabric resource, or connection is not allowed to access", LogLevel.Warning, false, details),
                401 or 403 => new(403, ErrorCodes.Fabric.Forbidden, "Connection is not allowed to access", LogLevel.Warning, false, details),
                429 => new(429, ErrorCodes.Fabric.RateLimited, "Reached rate limit, retry later", LogLevel.Warning, false, details),
                400 or 409 => new(status.Value, ErrorCodes.Fabric.RequestFailed, message, LogLevel.Warning, false, details),
                504 => new(504, ErrorCodes.Upstream.Timeout, "Fabric response reached timeout", LogLevel.Warning, false, details),
                _ => new(502, ErrorCodes.Fabric.Unavailable, "Fabric response with failed", LogLevel.Error, true, details)
            };
        }
    }
}
