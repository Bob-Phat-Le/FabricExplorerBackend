using FabricExplorerBackend.Application.Catalog.Contracts;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Catalog
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
