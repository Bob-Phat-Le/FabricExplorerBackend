using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.Notification;
using FabricExplorerBackend.Application.Notification.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Extensions;
using FabricExplorerBackend.Commons.Paginations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    [EnableCors("MyAllowSpecificOrigin")]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController(INotificationService service) : ControllerBase
    {
        /// <summary>Notification history of the current user, newest first.</summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<NotificationResponse>>>> ListNotificationsAsync(
            [FromQuery] PaginationRequest paginationRequest,
            [FromQuery] bool unreadOnly = false,
            CancellationToken cancellationToken = default)
            => (await service.GetNotificationsByUserIdAsync(User.GetUserId(), paginationRequest, unreadOnly, cancellationToken)).ToActionResult(this);

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
