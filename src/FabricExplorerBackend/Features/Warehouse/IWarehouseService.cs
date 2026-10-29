using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Features.Warehouse.Contracts;

namespace FabricExplorerBackend.Features.Warehouse
{
    public interface IWarehouseService
    {
        Task<Result<EventStreamingAdapter<WarehouseDetailResponse>>> GetWarehouseByIdAsync(Guid connectionId, Guid workspaceId, Guid warehouseId, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<WarehouseListItemResponse>>> GetAllWarehousesAsync(Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default);
        Task<Result<WarehouseConnectionStringResponse>> GetConnectionStringAsync(Guid connectionId, Guid workspaceId, Guid warehouseId, CancellationToken cancellationToken = default);
    }
}
