using FabricExplorerBackend.Application.Notification.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Application.Notification
{
    public class NotificationService(
        IUnitOfWork unitOfWork,
        INotificationMapper mapper,
        IEventRegistry registry,
        IEventPublisher publisher,
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

        public async Task<Result<List<NotificationResponse>>> GetNotificationsByUserIdAsync(Guid userId, int skip = 0, int take = 50, bool unreadOnly = false, CancellationToken cancellationToken = default)
        {
            var (notifications, _) = await unitOfWork.NotificationRepository.GetByUserIdAsync(
                userId, Math.Max(skip, 0), Math.Clamp(take, 1, MaxPageSize), false, cancellationToken, unreadOnly);

            return Result<List<NotificationResponse>>.Success(notifications.Select(mapper.Map).ToList());
        }

        public async Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
            => Result<int>.Success(await unitOfWork.NotificationRepository.GetUnreadCountAsync(userId, cancellationToken));

        public async Task<Result<NotificationResponse>> MarkAsReadAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
        {
            var notification = await unitOfWork.NotificationRepository.GetByIdAsync(id, true, cancellationToken);
            if (notification == null || notification.UserId != userId)
                return Error.NotFound(ErrorCodes.Notification.NotFound, "Notification not found");

            if (!notification.IsRead)
            {
                notification.MarkAsRead(clock.GetUtcNow());
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await PublishAsync(userId, null, cancellationToken);
            }

            return Result<NotificationResponse>.Success(mapper.Map(notification));
        }

        public async Task<Result<ConfirmationResponse>> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var updated = await unitOfWork.NotificationRepository.MarkAllAsReadAsync(userId, clock.GetUtcNow(), cancellationToken);
            if (updated > 0)
                await PublishAsync(userId, null, cancellationToken);

            return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = $"{updated} notification(s) marked as read" });
        }

        public async Task CreateAsync(Guid userId, NotificationTemplate template, bool succeeded, string subject, string? reason = null, Guid? operationId = null, CancellationToken cancellationToken = default)
        {
            Domain.Entities.Notification notification;
            try
            {
                var (type, title, message) = template.Render(succeeded, subject, reason);
                notification = Domain.Entities.Notification.Create(userId, type, title, message, operationId, clock.GetUtcNow());
                await unitOfWork.NotificationRepository.AddAsync(notification, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Cannot create notification {SucceededType} for user {UserId}", template.SucceededType, userId);
                return;
            }

            await PublishAsync(userId, notification, cancellationToken);
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
                await CreateAsync(userId, template, false, subject, "An unexpected error occurred.");
                throw;
            }

            await CreateAsync(userId, template, result.IsSuccess, subject, result.IsSuccess ? null : result.Errors[0].Message);
            return result;
        }

        public async Task<IReadOnlyList<Domain.Entities.Notification>> StageOperationResultAsync(FabricOperation operation, CancellationToken cancellationToken = default)
        {
            if (!operation.Status.IsTerminal())
                return [];

            try
            {
                var userIds = await unitOfWork.UserRepository.GetIdsByActiveConnectionAsync(operation.ConnectionId, cancellationToken);
                if (userIds.Count == 0)
                {
                    logger.LogInformation("Operation {OperationId} finished but no user is working on connection {ConnectionId}; no notification created",
                        operation.Id, operation.ConnectionId);
                    return [];
                }

                var (type, title, message) = NotificationTemplates.Operation.For(operation.OperationType).Render(
                    operation.Status == FabricOperationStatus.Succeeded,
                    operation.ResourceName ?? "requested",
                    operation.ErrorMessage);
                var now = clock.GetUtcNow();

                var staged = new List<Domain.Entities.Notification>(userIds.Count);
                foreach (var userId in userIds)
                {
                    var notification = Domain.Entities.Notification.Create(userId, type, title, message, operation.Id, now);
                    await unitOfWork.NotificationRepository.AddAsync(notification, cancellationToken);
                    staged.Add(notification);
                }

                return staged;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Cannot stage notification for operation {OperationId}", operation.Id);
                return [];
            }
        }

        public async Task<Result<EventStream>> OpenStreamAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var stream = registry.Create(userId.ToString(), NotificationStreamEvents.Channel);
            try
            {
                var unread = await unitOfWork.NotificationRepository.GetUnreadCountAsync(userId, cancellationToken);
                publisher.Publish(stream.Id, NotificationStreamEvents.UnreadCount, new NotificationStreamEvent(unread, null)); // [self-comment] This function just tells the eventStream to add all unread notifications into the List<StreamEvent>
            }
            catch
            {
                registry.Remove(stream.Id);
                throw;
            }

            return Result<EventStream>.Success(stream);
        }

        public void CloseStream(EventStream stream) => registry.Remove(stream.Id);

        public async Task PublishAsync(Guid userId, Domain.Entities.Notification? notification = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var unread = await unitOfWork.NotificationRepository.GetUnreadCountAsync(userId, cancellationToken);
                publisher.PublishToOwner(
                    userId.ToString(),
                    NotificationStreamEvents.Channel,
                    notification is null ? NotificationStreamEvents.UnreadCount : NotificationStreamEvents.Notification,
                    new NotificationStreamEvent(unread, notification is null ? null : mapper.Map(notification)));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Cannot publish notification event to user {UserId}", userId);
            }
        }
    }
}
