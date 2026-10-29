namespace FabricExplorerBackend.Features.Notification.Contracts
{
    public sealed record NotificationStreamEvent(int UnreadCount, NotificationResponse? Notification);
}
