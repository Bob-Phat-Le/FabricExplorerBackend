using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Models.Responses;
using Microsoft.Fabric.Api.Lakehouse.Models;
using FabricModels = Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Services.Interfaces
{
    public interface ILakehouseService
    {
        Task<ApiResponse<PagedResponse<IEnumerable<Table>>>> GetAllTablesAsync(Guid connectionId, Guid lakehouseId, PaginationRequest? paginationRequest);
        Task<ApiResponse<PagedResponse<ConfirmationResponse>>> LoadTableAsync(Guid connectionId, Guid lakehouseId, string tableName, LoadTableRequest request)
    }
}
