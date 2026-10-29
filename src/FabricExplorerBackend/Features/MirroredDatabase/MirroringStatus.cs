namespace FabricExplorerBackend.Features.MirroredDatabase
{
    public enum MirroringStatus
    {
        Initializing,
        Initialized,
        Starting,
        Running,
        Paused,
        Stopping,
        Stopped,
        Unknown,
        Checking
    }
}
