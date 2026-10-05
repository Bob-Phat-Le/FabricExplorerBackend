using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses;
using FabricExplorerBackend.Commons.Models.Responses.FabricClient;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public interface ILakehouseService
    {
        Task<Result<PagedResponseWithCursor<IEnumerable<TableResponse>>>> GetAllTablesAsync(Guid connectionId, Guid lakehouseId, PaginationWithCursorRequest? paginationRequest);
        Task<Result<ConfirmationResponse>> LoadTableAsync(Guid connectionId, Guid lakehouseId, string tableName, LoadTableRequest request);
    }
}
