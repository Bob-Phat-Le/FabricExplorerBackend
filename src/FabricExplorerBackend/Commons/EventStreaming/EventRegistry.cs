using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace FabricExplorerBackend.Commons.EventStreaming
{
    public sealed class EventRegistry(
        IOptions<EventStreamingOptions> options,
        TimeProvider clock,
        ILogger<EventRegistry> logger) : IEventRegistry
    {
        private readonly EventStreamingOptions options = options.Value;
        private readonly ConcurrentDictionary<string, EventStream> streams = new();

        public EventStream Create(string? ownerId, string? channel = null)
        {
            if (streams.Count >= options.MaxActiveStreams)
            {
                RemoveExpired();
                if (streams.Count >= options.MaxActiveStreams)
                    throw new Errors.ApplicationException(new Error(
                        ErrorKind.Unavailable, ErrorCodes.EventStream.LimitReached, "Too many active event streams, retry later"));
            }

            while (true)
            {
                var id = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));
                var stream = new EventStream(id, ownerId, options.MaxEventsPerStream, clock.GetUtcNow(), channel);
                if (streams.TryAdd(id, stream))
                    return stream;

                stream.Dispose();
            }
        }

        public IReadOnlyList<EventStream> FindByOwner(string ownerId, string channel)
        {
            return streams.Values
                .Where(s => !s.IsCompleted
                    && string.Equals(s.OwnerId, ownerId, StringComparison.Ordinal)
                    && string.Equals(s.Channel, channel, StringComparison.Ordinal))
                .ToList();
        }

        public bool TryGet(string streamId, out EventStream stream)
        {
            if (streams.TryGetValue(streamId, out var found))
            {
                stream = found;
                return true;
            }

            stream = null!;
            return false;
        }

        public bool Remove(string streamId)
        {
            if (!streams.TryRemove(streamId, out var stream))
                return false;

            stream.Dispose();
            return true;
        }

        public int RemoveExpired()
        {
            var now = clock.GetUtcNow();
            var removed = 0;

            foreach (var (id, stream) in streams)
            {
                var expired = stream.CompletedAt is { } completedAt
                    ? now - completedAt > options.CompletedRetention
                    : now - stream.CreatedAt > options.MaxLifetime;

                if (expired && Remove(id))
                    removed++;
            }

            if (removed > 0)
                logger.LogDebug("Removed {Count} expired event streams, {Active} remaining", removed, streams.Count);

            return removed;
        }
    }
}
