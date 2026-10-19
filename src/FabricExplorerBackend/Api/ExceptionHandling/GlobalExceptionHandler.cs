using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Commons.Errors;
using Microsoft.AspNetCore.Diagnostics;

namespace FabricExplorerBackend.Api.ExceptionHandling
{
    public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
        {
            if (http.Response.HasStarted) return false;

            if (ex is OperationCanceledException && http.RequestAborted.IsCancellationRequested)
            {
                logger.LogDebug("Request aborted by client: {Method} {Path}", http.Request.Method, http.Request.Path);
                http.Response.StatusCode = 499;
                return true;
            }

            var f = ExceptionClassifier.Classify(ex);
            var traceId = http.GetTraceId();

            using (logger.BeginScope(http.GetLogProperties()))
            {
                logger.Log(f.Level, f.LogStackTrace ? ex : null,
                    "{ErrorCode} {Method} {Path} -> {Status}. {ExceptionType}: {ExceptionMessage} {@Details}",
                    f.Code, http.Request.Method, http.Request.Path, f.Status, ex.GetType().Name, ex.Message, f.Details);
            }

            var message = f.Status >= 500 ? $"{f.Message} Mã tham chiếu: {traceId}" : f.Message;
            object? details = env.IsDevelopment() ? new { f.Details, exception = ex.ToString() } : f.Details;

            http.Response.StatusCode = f.Status;
            await http.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(f.Status, new ApiError(f.Code, message, traceId, details)), ct);
            return true;
        }
    }
}
