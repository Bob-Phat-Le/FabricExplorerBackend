namespace FabricExplorerBackend.Application.MirroredDatabase
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
