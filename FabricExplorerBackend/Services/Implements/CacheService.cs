using FabricExplorerBackend.Services.Interfaces;
using StackExchange.Redis;

namespace FabricExplorerBackend.Services.Implements
{
    public class CacheService(IConnectionMultiplexer muxer, IConfiguration configuration) : ICacheService
    {
        public string CreateCacheKey(params string[] keys)
        {
            return configuration.GetValue<string>("CacheSettings:BaseCacheKey") + ":" + string.Join(":", keys);
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
    }
}
