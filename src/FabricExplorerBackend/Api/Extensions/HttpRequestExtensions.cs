namespace FabricExplorerBackend.Api.Extensions
{
    public static class HttpRequestExtensions
    {
        public static bool ShouldBypassCache(this HttpRequest request)
            => request.Headers.CacheControl.ToString().Contains("no-cache", StringComparison.OrdinalIgnoreCase);
    }
}
