using FabricExplorerBackend.Infrastructures.Fabric.Dtos;

namespace FabricExplorerBackend.Application.Lakehouse
{
    public sealed record TableStats(
        long? RowCount,
        DateTimeOffset? LastModifiedTime,
        TableStatus Status,
        string? StatusMessage);

    public sealed record TableStatsSnapshot(
        IReadOnlyDictionary<string, TableStats> Ready,
        IReadOnlyList<string> Pending);

    public interface ILakehouseTableStatsService
    {
        Task<TableStatsSnapshot> GetCachedStatsAsync(
            Domain.Entities.Connection connection,
            Guid workspaceId,
            Guid lakehouseId,
            IEnumerable<FabricTableResponse> tables,
            bool bypassCache = false,
            CancellationToken cancellationToken = default);

        Task ComputeStatsAsync(
            Domain.Entities.Connection connection,
            Guid workspaceId,
            Guid lakehouseId,
            IReadOnlyCollection<string> tableNames,
            Func<string, TableStats, Task> onCompleted,
            bool bypassCache = false,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<string, TableStats>> GetStatsAsync(
            Domain.Entities.Connection connection,
            Guid workspaceId,
            Guid lakehouseId,
            IEnumerable<FabricTableResponse> tables,
            bool bypassCache = false,
            CancellationToken cancellationToken = default);
    }
}
