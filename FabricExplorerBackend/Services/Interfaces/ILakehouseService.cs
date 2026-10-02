using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Models.Responses;
using Microsoft.Fabric.Api.Lakehouse.Models;
using FabricModels = Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Services.Interfaces
{
    public interface ILakehouseService
    {
        Task<Result<PagedResponse<IEnumerable<Table>>>> GetAllTablesAsync(Guid connectionId, Guid lakehouseId, PaginationRequest? paginationRequest);
        Task<Result<ConfirmationResponse>> LoadTableAsync(Guid connectionId, Guid lakehouseId, string tableName, LoadTableRequest request);
    }
}
