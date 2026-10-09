namespace FabricExplorerBackend.Extensions
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Client gửi <c>Cache-Control: no-cache</c> (nút Reload/Refresh) nghĩa là muốn dữ liệu mới nhất:
        /// backend bỏ qua cache cũ rồi ghi đè cache bằng dữ liệu vừa lấy.
        /// </summary>
        public static bool ShouldBypassCache(this HttpRequest request)
            => request.Headers.CacheControl.ToString().Contains("no-cache", StringComparison.OrdinalIgnoreCase);
    }
}
