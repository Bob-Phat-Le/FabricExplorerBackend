namespace FabricExplorerBackend.Services.Interfaces
{
    public interface ICacheService
    {
        Task<object?> GetValueAsync(string key);
        Task SetValueAsync(string key, object value, DateTimeOffset? expiration = null);
        string CreateCacheKey(params string[] keys);
    }
}
