using System.Text.Json;
using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Commons.EventStreaming
{
    public sealed class EventPublisher(IEventRegistry registry, TimeProvider clock, ILogger<EventPublisher> logger) : IEventPublisher
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public bool Publish<T>(string streamId, string eventName, T payload)
        {
            if (!registry.TryGet(streamId, out var stream))
            {
                logger.LogDebug("Drop event {Event}: stream {StreamId} no longer exists", eventName, streamId);
                return false;
            }

            var appended = stream.TryAppend(eventName, JsonSerializer.Serialize(payload, JsonOptions), clock.GetUtcNow());
            if (!appended)
                logger.LogDebug("Drop event {Event}: stream {StreamId} is completed or full", eventName, streamId);

            return appended;
        }

        public void Complete(string streamId) => Publish(streamId, StreamEventNames.Completed, new { });

        public void Fail(string streamId, string code, string message)
            => Publish(streamId, StreamEventNames.Error, new StreamErrorPayload(code, message));
    }
}
