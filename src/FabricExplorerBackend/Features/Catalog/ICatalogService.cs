using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Features.Catalog.Contracts;

namespace FabricExplorerBackend.Features.Catalog
{
    public interface ICatalogService
    {
        Task<Result<CatalogTreeResponse>> GetCatalogTreeAsync(
            Guid connectionId, Guid workspaceId, Guid lakehouseId,
            bool bypassCache = false, CancellationToken cancellationToken = default);

        Task<Result<CatalogTableDetailResponse>> GetTableDetailAsync(
            Guid connectionId, Guid workspaceId, Guid lakehouseId, string schemaName, string tableName,
            bool bypassCache = false, CancellationToken cancellationToken = default);
    }
}
