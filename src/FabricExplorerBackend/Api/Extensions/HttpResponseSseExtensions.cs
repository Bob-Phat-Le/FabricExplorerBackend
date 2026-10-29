using FabricExplorerBackend.Commons.EventStreaming;
using Microsoft.AspNetCore.Http.Features;
using System.Text;

namespace FabricExplorerBackend.Api.Extensions
{
    public static class HttpResponseSseExtensions
    {
        public static async Task StreamEventsAsync(
            this HttpResponse response,
            EventStream stream,
            long afterEventId,
            TimeSpan heartbeatInterval,
            CancellationToken cancellationToken)
        {
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = "text/event-stream; charset=utf-8";
            response.Headers.CacheControl = "no-cache, no-store, no-transform";
            response.Headers["X-Accel-Buffering"] = "no"; // [self-comment] In case response must go through Nginx to get to client, Nginx normally will hold the response in buffer and waits until all responses are sent then forwards those to client. This header will let Nginx forwared the response to client right away when it receives a response.
            response.HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

            await response.StartAsync(cancellationToken);

            try
            {
                await response.WriteAsync(": connected\nretry: 3000\n\n", Encoding.UTF8, cancellationToken);
                await response.Body.FlushAsync(cancellationToken); // [self-comment] This function tells the server/framework/etc to try to flush the responses in buffer (if able), just understand this function makes the response goes to client right away.

                await foreach (var item in stream.ReadAsync(afterEventId, heartbeatInterval, cancellationToken))
                {
                    var frame = ReferenceEquals(item, StreamEvent.Heartbeat) ? ": heartbeat\n\n" : item.ToSseFrame();
                    await response.WriteAsync(frame, Encoding.UTF8, cancellationToken);
                    await response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public static string ToSseFrame(this StreamEvent item)
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
