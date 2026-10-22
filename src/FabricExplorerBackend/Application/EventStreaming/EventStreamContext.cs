using FabricExplorerBackend.Commons.EventStreaming;

namespace FabricExplorerBackend.Application.EventStreaming
{
    public sealed class EventStreamContext(
        string streamId,
        IEventPublisher publisher,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        public string StreamId { get; } = streamId;
        public IServiceProvider Services { get; } = services;
        public CancellationToken CancellationToken { get; } = cancellationToken;
        public bool Publish<T>(string eventName, T payload) => publisher.Publish(StreamId, eventName, payload);
    }
}
