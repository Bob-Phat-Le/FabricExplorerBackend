using System.Text.Json;

namespace FabricExplorerBackend.Commons.EventStreaming
{
    public class EventStreamingAdapter<T>
    {
        public bool IsEventStreaming => !string.IsNullOrEmpty(StreamId);
        public T? InnerData { get; set; }
        public string? StreamId { get; set; }
        public IReadOnlyList<string> PendingFields { get; set; } = [];
        public static EventStreamingAdapter<T> Ready(T data) => new() { InnerData = data };
        public static EventStreamingAdapter<T> Streaming(T data, string streamId, IReadOnlyList<string> pendingFields)
            => new()
            {
                InnerData = data,
                StreamId = streamId,
                PendingFields = pendingFields.Select(JsonNamingPolicy.CamelCase.ConvertName).ToList()
            };
    }
}
