using FabricExplorerBackend.Infrastructures.Fabric.Dtos;

namespace FabricExplorerBackend.Application.Lakehouse
{
    public sealed record TableStats(
        long? RowCount,
        DateTimeOffset? LastModifiedTime,
        TableStatus Status,
        string? StatusMessage);

    public interface ILakehouseTableStatsService
    {
        Task<IReadOnlyDictionary<string, TableStats>> GetStatsAsync(
            Domain.Entities.Connection connection,
            Guid workspaceId,
            Guid lakehouseId,
            IEnumerable<FabricTableResponse> tables,
            bool bypassCache = false,
            CancellationToken cancellationToken = default);
    }
}
