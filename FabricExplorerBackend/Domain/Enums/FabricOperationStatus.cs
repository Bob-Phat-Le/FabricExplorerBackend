namespace FabricExplorerBackend.Domain.Enums
{
    public enum FabricOperationStatus
    {
        Pending,
        Running,
        Succeeded,
        Failed,
        Cancelled,
        TimedOut,
        Unknown
    }

    public static class FabricOperationStatusExtensions
    {
        public static bool IsTerminal(this FabricOperationStatus status)
            => status is not (FabricOperationStatus.Pending or FabricOperationStatus.Running);
    }
}
