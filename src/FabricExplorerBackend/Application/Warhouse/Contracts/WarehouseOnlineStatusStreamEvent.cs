namespace FabricExplorerBackend.Application.Warhouse.Contracts
{
    public static class WarehouseStreamEvents
    {
        public const string OnlineStatus = "online-status";
    }

    public sealed record WarehouseOnlineStatusStreamEvent(string OnlineStatus, string? OnlineStatusMessage);
}
