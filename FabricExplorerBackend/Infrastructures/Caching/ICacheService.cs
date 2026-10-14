namespace FabricExplorerBackend.Infrastructures.Cache
{
    public interface ICacheService
    {
        Task<object?> GetValueAsync(string key);
        Task SetValueAsync(string key, object value, TimeSpan? expiration = null);
        string CreateCacheKey(params string[] keys);
        Task RemoveAsync(string key);
    }
}
