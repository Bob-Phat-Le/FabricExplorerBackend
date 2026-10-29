using FabricExplorerBackend.Features.Notification.Contracts;

namespace FabricExplorerBackend.Features.Notification
{
    public partial class NotificationMapper : INotificationMapper
    {
        public NotificationResponse Map(Domain.Entities.Notification notification)
        {
            return new NotificationResponse
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                OperationId = notification.OperationId,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt
            };
        }
    }
}
