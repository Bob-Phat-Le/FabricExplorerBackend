namespace FabricExplorerBackend.Features.MirroredDatabase
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
