using FabricExplorerBackend.Application.Notification.Contracts;

namespace FabricExplorerBackend.Application.Notification
{
    public interface INotificationMapper
    {
        NotificationResponse Map(Domain.Entities.Notification notification);
    }
}
