using FabricExplorerBackend.Configurations.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FabricExplorerBackend.Infrastructures.Cache
{
    public class CacheService(IConnectionMultiplexer muxer, IOptions<CacheOptions> cacheOptions) : ICacheService
    {
        private CacheOptions cacheOptions = cacheOptions.Value;

        public string CreateCacheKey(params string[] keys)
        {
            return cacheOptions.KeyPrefix + ":" + string.Join(":", keys);
        }

        public async Task<object?> GetValueAsync(string key)
        {
            var redis = muxer.GetDatabase();
            var value = await redis.StringGetAsync(key);
            return value;
        }

        public async Task SetValueAsync(string key, object value, TimeSpan? expiration = null)
        {
            var redis = muxer.GetDatabase();
            if (expiration.HasValue)
            {
                await redis.StringSetAsync(key, value.ToString(), expiration.Value);
                return;
            }
            await redis.StringSetAsync(key, value.ToString());
        }

        public async Task RemoveAsync(string key)
            => await muxer.GetDatabase().KeyDeleteAsync(key);
    }
}
