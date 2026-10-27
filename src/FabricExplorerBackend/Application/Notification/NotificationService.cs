using FabricExplorerBackend.Application.Notification.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Application.Notification
{
    public class NotificationService(
        IEventPublisher publisher,
        IUnitOfWork unitOfWork,
        INotificationMapper mapper,
        TimeProvider clock,
        ILogger<NotificationService> logger) : INotificationService
    {
        private const int MaxPageSize = 100;

        public async Task<Result<NotificationResponse>> GetNotificationByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
        {
            var notification = await unitOfWork.NotificationRepository.GetByIdAsync(id, false, cancellationToken);
            if (notification == null || notification.UserId != userId)
            {
                logger.LogWarning("Notification with id {NotificationId} not found for user {UserId}", id, userId);
                return Error.NotFound(ErrorCodes.Notification.NotFound, "Notification not found");
            }

            return Result<NotificationResponse>.Success(mapper.Map(notification));
        }

        public async Task<Result<List<NotificationResponse>>> GetNotificationsByUserIdAsync(Guid userId, PaginationRequest? paginationRequest, bool unreadOnly = false, CancellationToken cancellationToken = default)
        {
            if (paginationRequest != null)
                paginationRequest.Validate();

            var page = paginationRequest?.Page ?? 1;
            var pageSize = paginationRequest!.PageSize;

            var (notifications, _) = await unitOfWork.NotificationRepository.GetByUserIdAsync(
                userId, (page-1)*pageSize, pageSize, false, cancellationToken, unreadOnly);

            return Result<List<NotificationResponse>>.Success(notifications.Select(mapper.Map).ToList());
        }

        public async Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
            => Result<int>.Success(await unitOfWork.NotificationRepository.GetUnreadCountAsync(userId, cancellationToken));

        public async Task<Result<NotificationResponse>> MarkAsReadAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
        {
            var notification = await unitOfWork.NotificationRepository.GetByIdAsync(id, true, cancellationToken);
            if (notification == null || notification.UserId != userId)
                return Error.NotFound(ErrorCodes.Notification.NotFound, "Notification not found");

            // Idempotent: an already-read notification keeps its original ReadAt.
            if (!notification.IsRead)
            {
                notification.MarkAsRead(clock.GetUtcNow());
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result<NotificationResponse>.Success(mapper.Map(notification));
        }

        public async Task<Result<ConfirmationResponse>> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var updated = await unitOfWork.NotificationRepository.MarkAllAsReadAsync(userId, clock.GetUtcNow(), cancellationToken);
            return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = $"{updated} notification(s) marked as read" });
        }

        public async Task CreateAsync(Guid userId, NotificationTemplate template, bool succeeded, string subject, string? reason = null, Guid? operationId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var (type, title, message) = template.Render(succeeded, subject, reason);
                await unitOfWork.NotificationRepository.AddAsync(
                    Domain.Entities.Notification.Create(userId, type, title, message, operationId, clock.GetUtcNow()), cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                try
                {
                    var streamId = Guid.NewGuid().ToString();
                    var unreadCount = await unitOfWork.NotificationRepository.GetUnreadCountAsync(userId, cancellationToken);
                    publisher.Publish(streamId, "Notification changes", unreadCount);

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cannot publish notification");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Cannot create notification {SucceededType} for user {UserId}", template.SucceededType, userId);
            }
        }

        public async Task<Result<T>> ExecuteWithNotificationAsync<T>(Guid userId, NotificationTemplate template, string subject, Func<Task<Result<T>>> action)
        {
            Result<T> result;
            try
            {
                result = await action();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Exception text is deliberately not copied into the notification (may contain tenant/client details).
                await CreateAsync(userId, template, false, subject, "An unexpected error occurred.");
                throw;
            }

            // No cancellation token: the outcome is reported even if the HTTP request was aborted meanwhile.
            await CreateAsync(userId, template, result.IsSuccess, subject, result.IsSuccess ? null : result.Errors[0].Message);
            return result;
        }

        public async Task StageOperationResultAsync(FabricOperation operation, CancellationToken cancellationToken = default)
        {
            if (!operation.Status.IsTerminal())
                return;

            try
            {
                // An operation has no owner column, so it is reported to the users working on its connection.
                var userIds = await unitOfWork.UserRepository.GetIdsByActiveConnectionAsync(operation.ConnectionId, cancellationToken);
                if (userIds.Count == 0)
                {
                    logger.LogInformation("Operation {OperationId} finished but no user is working on connection {ConnectionId}; no notification created",
                        operation.Id, operation.ConnectionId);
                    return;
                }

                // Only a confirmed success is "succeeded". Failed, TimedOut, Cancelled and Unknown
                // (outcome could not be confirmed) all need the user's attention.
                var (type, title, message) = NotificationTemplates.Operation.For(operation.OperationType).Render(
                    operation.Status == FabricOperationStatus.Succeeded,
                    operation.ResourceName ?? "requested",
                    operation.ErrorMessage);
                var now = clock.GetUtcNow();

                foreach (var userId in userIds)
                    await unitOfWork.NotificationRepository.AddAsync(
                        Domain.Entities.Notification.Create(userId, type, title, message, operation.Id, now), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never block the operation status update because of its notification.
                logger.LogError(ex, "Cannot stage notification for operation {OperationId}", operation.Id);
            }
        }
    }
}
