namespace FabricExplorerBackend.Infrastructures.Cache
{
    public interface ISingleFlight
    {
        Task<T> RunAsync<T>(string key, Func<Task<T>> work, CancellationToken cancellationToken = default);
    }
}
