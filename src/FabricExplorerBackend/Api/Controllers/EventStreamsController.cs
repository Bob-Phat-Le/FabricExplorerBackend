using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.EventStreaming;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text;

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
            var after = lastEventId ?? ParseLastEventIdHeader();

            if (stream.IsCompleted && after >= stream.LastEventId)
                return NoContent();

            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/event-stream; charset=utf-8";
            Response.Headers.CacheControl = "no-cache, no-store, no-transform";
            Response.Headers["X-Accel-Buffering"] = "no";
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

            await Response.StartAsync(cancellationToken);

            try
            {
                await WriteRawAsync(": connected\nretry: 3000\n\n", cancellationToken);

                await foreach (var item in stream.ReadAsync(after, options.Value.HeartbeatInterval, cancellationToken))
                {
                    var frame = ReferenceEquals(item, StreamEvent.Heartbeat) ? ": heartbeat\n\n" : FormatFrame(item);
                    await WriteRawAsync(frame, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }

            return new EmptyResult();
        }

        private long ParseLastEventIdHeader()
            => long.TryParse(Request.Headers["Last-Event-ID"].ToString(), out var id) && id > 0 ? id : 0;

        private async Task WriteRawAsync(string text, CancellationToken cancellationToken)
        {
            await Response.WriteAsync(text, Encoding.UTF8, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        private static string FormatFrame(StreamEvent item)
        {
            var sb = new StringBuilder();
            sb.Append("id: ").Append(item.Id).Append('\n');
            sb.Append("event: ").Append(item.Name).Append('\n');
            foreach (var line in item.Data.Split('\n'))
                sb.Append("data: ").Append(line.TrimEnd('\r')).Append('\n');
            sb.Append('\n');
            return sb.ToString();
        }
    }
}
