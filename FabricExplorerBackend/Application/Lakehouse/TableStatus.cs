namespace FabricExplorerBackend.Application.Lakehouse
{
    public enum TableStatus
    {
        Healthy,
        Empty,
        Stale,
        Unknown,
        Error
    }
}
