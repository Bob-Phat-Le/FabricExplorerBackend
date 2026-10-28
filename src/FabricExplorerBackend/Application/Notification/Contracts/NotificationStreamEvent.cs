namespace FabricExplorerBackend.Application.Notification.Contracts
{
    public sealed record NotificationStreamEvent(int UnreadCount, NotificationResponse? Notification);
}
