namespace FabricExplorerBackend.Domain.Enums
{
    public enum TableMirroringStatus
    {
        Initialized,
        Snapshotting,
        Replicating,
        Reseeding,
        Stopped,
        Failed,
        Unknown
    }
}
