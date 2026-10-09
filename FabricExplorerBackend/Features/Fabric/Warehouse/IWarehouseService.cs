using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Warehouse;

namespace FabricExplorerBackend.Features.Fabric.Warehouse
{
    public interface IWarehouseService
    {
        Task<Result<WarehouseDetailResponse>> GetWarehouseByIdAsync(Guid connectionId, Guid workspaceId, Guid warehouseId, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<WarehouseListItemResponse>>> GetAllWarehousesAsync(Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default);
        Task<Result<WarehouseConnectionStringResponse>> GetConnectionStringAsync(Guid connectionId, Guid workspaceId, Guid warehouseId, CancellationToken cancellationToken = default);
    }
}
