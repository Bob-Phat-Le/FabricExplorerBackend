using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
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
            Guid lakehouseId,
            IEnumerable<FabricTableResponse> tables,
            CancellationToken cancellationToken = default);
    }
}
