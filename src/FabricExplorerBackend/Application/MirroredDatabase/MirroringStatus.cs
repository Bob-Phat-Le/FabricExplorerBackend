namespace FabricExplorerBackend.Application.MirroredDatabase
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
