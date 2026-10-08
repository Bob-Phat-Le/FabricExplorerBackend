using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Paginations;
using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
using FabricExplorerBackend.Commons.Models.Responses.Lakehouse;
using FabricExplorerBackend.Commons.Models.Responses.Paginations;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public interface ILakehouseService
    {
        Task<Result<PagedResponseWithCursor<IEnumerable<TableResponse>>>> GetAllTablesAsync(Guid connectionId, Guid lakehouseId, PaginationWithCursorRequest? paginationRequest);
        Task<Result<ConfirmationResponse>> LoadTableAsync(Guid connectionId, Guid lakehouseId, string tableName, LoadTableRequest request);
        Task<Result<IEnumerable<LakehouseListItemResponse>>> GetAllLakehousesAsync(Guid connectionId, Guid workspaceId);
    }
}
