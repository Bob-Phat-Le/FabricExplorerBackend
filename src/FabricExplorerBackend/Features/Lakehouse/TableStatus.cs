namespace FabricExplorerBackend.Features.Lakehouse
{
    public enum TableStatus
    {
        Healthy,
        Empty,
        Stale,
        Unknown,
        Error,
        Checking
    }
}
