using System.Collections.Concurrent;

namespace FabricExplorerBackend.Features.Cache
{
    // Phải đăng ký Singleton: danh sách các lần chạy đang diễn ra phải dùng chung cho mọi request
    public sealed class SingleFlight : ISingleFlight
    {
        private readonly ConcurrentDictionary<string, object> inflight = new();

        public Task<T> RunAsync<T>(string key, Func<Task<T>> work, CancellationToken cancellationToken = default)
        {
            Lazy<Task<T>>? created = null;
            // Lazy đảm bảo work chỉ chạy một lần dù GetOrAdd có gọi factory nhiều lần khi tranh chấp
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
                // Chỉ gỡ đúng lần chạy này (không gỡ nhầm lần chạy mới hơn cùng key)
                inflight.TryRemove(new KeyValuePair<string, object>(key, self));
            }
        }
    }
}
