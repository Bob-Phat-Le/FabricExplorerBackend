using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace FabricExplorerBackend.Application.QueryService
{
    public sealed class QueryJobQueue : IQueryJobQueue
    {
        private readonly Channel<QueryJob> channel;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> running = new();

        public QueryJobQueue(IOptions<QueryOptions> options)
        {
            channel = Channel.CreateBounded<QueryJob>(new BoundedChannelOptions(Math.Max(1, options.Value.QueueCapacity))
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
        }

        public bool TryEnqueue(QueryJob job) => channel.Writer.TryWrite(job);

        public IAsyncEnumerable<QueryJob> DequeueAsync(CancellationToken cancellationToken)
            => channel.Reader.ReadAllAsync(cancellationToken);

        public CancellationTokenSource BeginRun(Guid queryId, CancellationToken stoppingToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            running[queryId] = cts;
            return cts;
        }

        public bool TryCancel(Guid queryId)
        {
            if (!running.TryGetValue(queryId, out var cts))
                return false;

            try
            {
                cts.Cancel();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public void EndRun(Guid queryId) => running.TryRemove(queryId, out _);
    }
}
