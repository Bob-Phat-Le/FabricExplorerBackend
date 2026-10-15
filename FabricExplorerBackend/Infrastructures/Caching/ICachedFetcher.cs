namespace FabricExplorerBackend.Infrastructures.Cache
{
    public interface ICachedFetcher
    {
        string CreateKey(params string[] parts);

        Task<T> GetOrFetchAsync<T>(
            string key,
            TimeSpan ttl,
            Func<CancellationToken, Task<T>> fetch,
            bool bypassCache = false,
            Func<T, bool>? cacheWhen = null,
            CancellationToken cancellationToken = default);
    }
}
