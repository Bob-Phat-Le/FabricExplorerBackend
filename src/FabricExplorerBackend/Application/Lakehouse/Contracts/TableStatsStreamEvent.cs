namespace FabricExplorerBackend.Application.Lakehouse.Contracts
{
    public static class LakehouseStreamEvents
    {
        public const string TableStats = "table-stats";
    }

    public sealed record TableStatsStreamEvent(
        string Name,
        long? RowCount,
        DateTimeOffset? LastModifiedTime,
        string Status,
        string? StatusMessage)
    {
        public static TableStatsStreamEvent From(string tableName, TableStats stats)
            => new(tableName, stats.RowCount, stats.LastModifiedTime, stats.Status.ToString(), stats.StatusMessage);
    }
}
