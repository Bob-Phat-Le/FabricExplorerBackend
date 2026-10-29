using FabricExplorerBackend.Features.Notification.Contracts;

namespace FabricExplorerBackend.Features.Notification
{
    public interface INotificationMapper
    {
        NotificationResponse Map(Domain.Entities.Notification notification);
    }
}
