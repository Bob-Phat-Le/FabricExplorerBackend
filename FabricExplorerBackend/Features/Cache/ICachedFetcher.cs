namespace FabricExplorerBackend.Features.Cache
{
    /// <summary>
    /// Đọc-qua-cache (Redis) cho dữ liệu lấy từ Fabric, kèm gộp các lời gọi đồng thời (single-flight).
    /// Thứ tự: cache -> (chưa có) một lần fetch duy nhất dùng chung -> ghi cache.
    /// </summary>
    public interface ICachedFetcher
    {
        string CreateKey(params string[] parts);

        /// <param name="fetch">Chạy tối đa một lần cho mỗi key tại một thời điểm; token truyền vào là token timeout riêng, KHÔNG phải token của request. Xem quy tắc ở ISingleFlight.</param>
        /// <param name="bypassCache">true = bỏ qua cache cũ, lấy mới và ghi đè cache (dùng cho nút Reload/Refresh).</param>
        /// <param name="cacheWhen">Chỉ ghi cache khi trả true (vd. không cache kết quả lỗi). Giá trị null không bao giờ được cache.</param>
        /// <param name="cancellationToken">Chỉ hủy việc chờ của người gọi này.</param>
        Task<T> GetOrFetchAsync<T>(
            string key,
            TimeSpan ttl,
            Func<CancellationToken, Task<T>> fetch,
            bool bypassCache = false,
            Func<T, bool>? cacheWhen = null,
            CancellationToken cancellationToken = default);
    }
}
