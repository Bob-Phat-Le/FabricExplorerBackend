namespace FabricExplorerBackend.Features.Cache
{
    public interface ICacheService
    {
        Task<object?> GetValueAsync(string key);
        Task SetValueAsync(string key, object value, TimeSpan? expiration = null);
        string CreateCacheKey(params string[] keys);
    }
}
