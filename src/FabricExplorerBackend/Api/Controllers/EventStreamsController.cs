using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.EventStreaming;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Api.Controllers
{
    [Route("api/event-streams")]
    [ApiController]
    public class EventStreamsController(
        IEventStreamService eventStreamService,
        IOptions<EventStreamingOptions> options) : ControllerBase
    {
        [HttpGet("{streamId}", Name = "GetEventStream")]
        [Produces("text/event-stream")]
        public async Task<IActionResult> GetEventStream(
            string streamId,
            [FromQuery] long? lastEventId,
            CancellationToken cancellationToken)
        {
            var opened = eventStreamService.Open(streamId);
            if (!opened.IsSuccess)
                return Result<object>.Failure(opened.Errors[0]).ToActionResult(this).Result!;

            var stream = opened.Data!;
            var after = lastEventId ?? (long.TryParse(Request.Headers["Last-Event-ID"].ToString(), out var id) && id > 0 ? id : 0);

            if (stream.IsCompleted && after >= stream.LastEventId)
                return NoContent();

            await Response.StreamEventsAsync(stream, after, options.Value.HeartbeatInterval, cancellationToken);

            return new EmptyResult();
        }
    }
}
