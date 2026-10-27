using FabricExplorerBackend.Application.Notification.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Application.Notification
{
    public interface INotificationService
    {
        // ---- Read side: always scoped to the given user, a user can never see another user's notifications ----
        Task<Result<NotificationResponse>> GetNotificationByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
        Task<Result<List<NotificationResponse>>> GetNotificationsByUserIdAsync(Guid userId, PaginationRequest? request, bool unreadOnly = false, CancellationToken cancellationToken = default);
        Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

        // ---- Read / unread ----
        Task<Result<NotificationResponse>> MarkAsReadAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
        Task<Result<ConfirmationResponse>> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

        // ---- Write side ----

        /// <summary>
        /// Creates and saves the notification of a business event. Best-effort: a notification problem is logged and
        /// never thrown, so it cannot break the business action that triggered it.
        /// </summary>
        Task CreateAsync(Guid userId, NotificationTemplate template, bool succeeded, string subject, string? reason = null, Guid? operationId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs a business action and reports its outcome to the user: a notification for success, one for a failed
        /// <see cref="Result{T}"/>, and one for an exception (which is rethrown unchanged).
        /// </summary>
        Task<Result<T>> ExecuteWithNotificationAsync<T>(Guid userId, NotificationTemplate template, string subject, Func<Task<Result<T>>> action);

        /// <summary>
        /// Adds (does NOT save) the notification of an operation that has just reached a terminal status.
        /// The caller saves, so the status change and its notification commit atomically.
        /// The operation status itself stays owned by the operation processor.
        /// </summary>
        Task StageOperationResultAsync(FabricOperation operation, CancellationToken cancellationToken = default);
    }
}
