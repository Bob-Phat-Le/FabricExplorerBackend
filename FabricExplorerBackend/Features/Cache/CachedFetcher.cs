using System.Text.Json;

namespace FabricExplorerBackend.Features.Cache
{
    public class CachedFetcher(
        ICacheService cacheService,
        ISingleFlight singleFlight,
        IConfiguration configuration,
        ILogger<CachedFetcher> logger) : ICachedFetcher
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public string CreateKey(params string[] parts) => cacheService.CreateCacheKey(parts);

        public async Task<T> GetOrFetchAsync<T>(
            string key,
            TimeSpan ttl,
            Func<CancellationToken, Task<T>> fetch,
            bool bypassCache = false,
            Func<T, bool>? cacheWhen = null,
            CancellationToken cancellationToken = default)
        {
            if (!bypassCache)
            {
                var (found, cached) = await TryReadAsync<T>(key);
                if (found) return cached!;
            }

            var timeout = TimeSpan.FromSeconds(configuration.GetValue("Fabric:SharedFetchTimeoutSeconds", 60));

            return await singleFlight.RunAsync(key, async () =>
            {
                // Có thể một lần fetch khác vừa ghi cache xong trong lúc ta xếp hàng
                if (!bypassCache)
                {
                    var (found, cached) = await TryReadAsync<T>(key);
                    if (found) return cached!;
                }

                // Fetch dùng chung nên không gắn với token của một request cụ thể; thay bằng timeout để không treo vô hạn
                using var timeoutSource = new CancellationTokenSource(timeout);
                T value;
                try
                {
                    value = await fetch(timeoutSource.Token);
                }
                catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
                {
                    throw new TimeoutException($"Fetching data from Fabric timed out after {timeout.TotalSeconds:0} seconds.");
                }

                if (value is not null && (cacheWhen?.Invoke(value) ?? true))
                    await TryWriteAsync(key, value, ttl);

                return value;
            }, cancellationToken);
        }

        // Redis chỉ là lớp tăng tốc: lỗi cache không được làm hỏng request, chỉ quay về lấy trực tiếp từ Fabric
        private async Task<(bool Found, T? Value)> TryReadAsync<T>(string key)
        {
            try
            {
                var raw = (await cacheService.GetValueAsync(key))?.ToString();
                if (string.IsNullOrEmpty(raw)) return (false, default);

                var value = JsonSerializer.Deserialize<T>(raw, JsonOptions);
                return value is null ? (false, default) : (true, value);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot read cache entry {CacheKey}.", key);
                return (false, default);
            }
        }

        private async Task TryWriteAsync<T>(string key, T value, TimeSpan ttl)
        {
            try
            {
                await cacheService.SetValueAsync(key, JsonSerializer.Serialize(value, JsonOptions), ttl);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot write cache entry {CacheKey}.", key);
            }
        }
    }
}
