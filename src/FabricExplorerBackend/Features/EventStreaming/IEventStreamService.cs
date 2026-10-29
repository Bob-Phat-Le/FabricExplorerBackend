using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Features.EventStreaming
{
    public interface IEventStreamService
    {
        EventStreamingAdapter<T> Stream<T>(T data, IReadOnlyList<string> pendingFields, Func<EventStreamContext, Task> work);
        Result<EventStream> Open(string streamId);
    }
}
