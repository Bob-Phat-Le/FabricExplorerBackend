using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Commons.Results;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Application.Lakehouse
{
    public interface ILakehouseService
    {
        Task<Result<PagedResponseWithCursor<IEnumerable<TableResponse>>>> GetAllTablesAsync(
            Guid connectionId, Guid workspaceId, Guid lakehouseId, PaginationWithCursorRequest? paginationRequest,
            bool bypassCache = false, CancellationToken cancellationToken = default);
        Task<Result<ConfirmationResponse>> LoadTableAsync(Guid connectionId, Guid lakehouseId, string tableName, LoadTableRequest request);
        Task<Result<IEnumerable<LakehouseListItemResponse>>> GetAllLakehousesAsync(
            Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default);
    }
}
