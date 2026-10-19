using FabricExplorerBackend.Api.Extensions;

namespace FabricExplorerBackend.Api.Middlewares
{
    public sealed class RequestContextMiddleware(RequestDelegate next, ILogger<RequestContextMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext http)
        {
            http.Response.OnStarting(() =>
            {
                http.Response.Headers["X-Trace-Id"] = http.GetTraceId();
                return Task.CompletedTask;
            });

            // Mọi logger.Log... trong request này (service, repository...) đều tự mang các property trên
            using (logger.BeginScope(http.GetLogProperties()))
                await next(http);
        }
    }
}
