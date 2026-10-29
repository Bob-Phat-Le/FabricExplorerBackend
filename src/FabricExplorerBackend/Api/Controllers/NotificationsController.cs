using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.Notification;
using FabricExplorerBackend.Application.Notification.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Extensions;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Api.Controllers
{
    [EnableCors("MyAllowSpecificOrigin")]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController(
        INotificationService service,
        IOptions<EventStreamingOptions> streamingOptions) : ControllerBase
    {
        [HttpGet("stream")]
        [Produces("text/event-stream")]
        public async Task<IActionResult> StreamAsync(CancellationToken cancellationToken)
        {
            var opened = await service.OpenStreamAsync(User.GetUserId(), cancellationToken); // [self-comment] This retrieve unread notification in database then append it in an eventStream
            if (!opened.IsSuccess)
                return Result<object>.Failure(opened.Errors[0]).ToActionResult(this).Result!;

            var stream = opened.Data!;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stream.Lifetime);
            linked.CancelAfter(streamingOptions.Value.MaxLifetime * 0.8);

            try
            {
                await Response.StreamEventsAsync(stream, 0, streamingOptions.Value.HeartbeatInterval, linked.Token);
            }
            finally
            {
                service.CloseStream(stream);
            }

            return new EmptyResult();
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<NotificationResponse>>>> ListNotificationsAsync(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50,
            [FromQuery] bool unreadOnly = false,
            CancellationToken cancellationToken = default)
            => (await service.GetNotificationsByUserIdAsync(User.GetUserId(), skip, take, unreadOnly, cancellationToken)).ToActionResult(this);

        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCountAsync(CancellationToken cancellationToken = default)
            => (await service.GetUnreadCountAsync(User.GetUserId(), cancellationToken)).ToActionResult(this);

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<NotificationResponse>>> GetNotificationByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => (await service.GetNotificationByIdAsync(User.GetUserId(), id, cancellationToken)).ToActionResult(this);

        [HttpPost("{id:guid}/read")]
        public async Task<ActionResult<ApiResponse<NotificationResponse>>> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
            => (await service.MarkAsReadAsync(User.GetUserId(), id, cancellationToken)).ToActionResult(this);

        [HttpPost("read-all")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
            => (await service.MarkAllAsReadAsync(User.GetUserId(), cancellationToken)).ToActionResult(this);
    }
}
