using FabricExplorerBackend.Application.Notification.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Application.Notification
{
    public interface INotificationService
    {
        Task<Result<NotificationResponse>> GetNotificationByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
        Task<Result<List<NotificationResponse>>> GetNotificationsByUserIdAsync(Guid userId, int skip = 0, int take = 50, bool unreadOnly = false, CancellationToken cancellationToken = default);
        Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result<NotificationResponse>> MarkAsReadAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
        Task<Result<ConfirmationResponse>> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

        Task CreateAsync(Guid userId, NotificationTemplate template, bool succeeded, string subject, string? reason = null, Guid? operationId = null, CancellationToken cancellationToken = default);

        Task<Result<T>> ExecuteWithNotificationAsync<T>(Guid userId, NotificationTemplate template, string subject, Func<Task<Result<T>>> action);

        Task<IReadOnlyList<Domain.Entities.Notification>> StageOperationResultAsync(FabricOperation operation, CancellationToken cancellationToken = default);

        Task<Result<EventStream>> OpenStreamAsync(Guid userId, CancellationToken cancellationToken = default);

        void CloseStream(EventStream stream);

        Task PublishAsync(Guid userId, Domain.Entities.Notification? notification = null, CancellationToken cancellationToken = default);

        Task<Result<ConfirmationResponse>> DeleteNotificationByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    }
}
