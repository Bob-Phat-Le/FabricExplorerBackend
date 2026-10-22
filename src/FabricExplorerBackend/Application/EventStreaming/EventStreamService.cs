using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.EventStreaming
{
    public sealed class EventStreamService(
        IEventRegistry registry,
        IEventPublisher publisher,
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime applicationLifetime,
        IHttpContextAccessor httpContextAccessor,
        ILogger<EventStreamService> logger) : IEventStreamService
    {
        public EventStreamingAdapter<T> Stream<T>(T data, IReadOnlyList<string> pendingFields, Func<EventStreamContext, Task> work)
        {
            var stream = registry.Create(GetCurrentUserId());

            _ = Task.Run(() => RunAsync(stream, work));

            return EventStreamingAdapter<T>.Streaming(data, stream.Id, pendingFields);
        }

        public Result<EventStream> Open(string streamId)
        {
            if (!registry.TryGet(streamId, out var stream))
                return Error.NotFound(ErrorCodes.EventStream.NotFound, "Event stream not found or expired");

            var currentUserId = GetCurrentUserId();
            if (stream.OwnerId is not null && !string.Equals(stream.OwnerId, currentUserId, StringComparison.Ordinal))
                return Error.NotFound(ErrorCodes.EventStream.NotFound, "Event stream not found or expired");

            return Result<EventStream>.Success(stream);
        }

        private async Task RunAsync(EventStream stream, Func<EventStreamContext, Task> work)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(stream.Lifetime, applicationLifetime.ApplicationStopping);
            try
            {
                using var scope = scopeFactory.CreateScope();
                await work(new EventStreamContext(stream.Id, publisher, scope.ServiceProvider, linked.Token));
                publisher.Complete(stream.Id);
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested)
            {
                logger.LogDebug("Stream work {StreamId} cancelled (stream expired or application stopping)", stream.Id);
            }
            catch (Exception ex)
            {
                var failure = ExceptionClassifier.Classify(ex);
                logger.Log(failure.Level, failure.LogStackTrace ? ex : null,
                    "Stream work {StreamId} failed. {ErrorCode}: {ExceptionType}: {ExceptionMessage}",
                    stream.Id, failure.Code, ex.GetType().Name, ex.Message);
                publisher.Fail(stream.Id, failure.Code, failure.Message);
            }
        }

        private string? GetCurrentUserId()
            => httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value;
    }
}
