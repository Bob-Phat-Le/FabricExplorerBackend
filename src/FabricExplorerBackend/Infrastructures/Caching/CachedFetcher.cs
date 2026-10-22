using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FabricExplorerBackend.Infrastructures.Cache
{
    public class CachedFetcher(
        ICacheService cacheService,
        ISingleFlight singleFlight,
        IOptions<CacheOptions> cacheOptions,
        ILogger<CachedFetcher> logger) : ICachedFetcher
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private CacheOptions cacheOptions = cacheOptions.Value;

        public string CreateKey(params string[] parts) => cacheService.CreateCacheKey(parts);

        public Task<(bool Found, T? Value)> TryGetAsync<T>(string key) => TryReadAsync<T>(key);

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

            var timeout = cacheOptions.SharedFetchTimeout;

            return await singleFlight.RunAsync(key, async () =>
            {
                if (!bypassCache)
                {
                    var (found, cached) = await TryReadAsync<T>(key);
                    if (found) return cached!;
                }

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

                return value;
            }, cancellationToken);
        }

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

        //private async Task TryWriteAsync<T>(string key, T value, TimeSpan ttl)
        //{
        //    try
        //    {
        //        await cacheService.SetValueAsync(key, JsonSerializer.Serialize(value, JsonOptions), ttl);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogWarning(ex, "Cannot write cache entry {CacheKey}.", key);
        //    }
        //}
    }
}
