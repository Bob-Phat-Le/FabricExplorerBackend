using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public interface ILakehouseService
    {
        Task<Result<PagedResponse<IEnumerable<Table>>>> GetAllTablesAsync(Guid connectionId, Guid lakehouseId, PaginationRequest? paginationRequest);
        Task<Result<ConfirmationResponse>> LoadTableAsync(Guid connectionId, Guid lakehouseId, string tableName, LoadTableRequest request);
    }
}
