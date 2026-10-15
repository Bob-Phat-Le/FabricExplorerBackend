using System.Collections.Concurrent;

namespace FabricExplorerBackend.Infrastructures.Cache
{
    public sealed class SingleFlight : ISingleFlight
    {
        private readonly ConcurrentDictionary<string, object> inflight = new();

        public Task<T> RunAsync<T>(string key, Func<Task<T>> work, CancellationToken cancellationToken = default)
        {
            Lazy<Task<T>>? created = null;
            created = new Lazy<Task<T>>(() => ExecuteAsync(key, work, created!));

            var flight = (Lazy<Task<T>>)inflight.GetOrAdd(key, created);
            return flight.Value.WaitAsync(cancellationToken);
        }

        private async Task<T> ExecuteAsync<T>(string key, Func<Task<T>> work, Lazy<Task<T>> self)
        {
            try
            {
                return await work();
            }
            finally
            {
                inflight.TryRemove(new KeyValuePair<string, object>(key, self));
            }
        }
    }
}
