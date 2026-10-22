using FabricExplorerBackend.Application.Warhouse.Contracts;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Warhouse
{
    public interface IWarehouseService
    {
        Task<Result<EventStreamingAdapter<WarehouseDetailResponse>>> GetWarehouseByIdAsync(Guid connectionId, Guid workspaceId, Guid warehouseId, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<WarehouseListItemResponse>>> GetAllWarehousesAsync(Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default);
        Task<Result<WarehouseConnectionStringResponse>> GetConnectionStringAsync(Guid connectionId, Guid workspaceId, Guid warehouseId, CancellationToken cancellationToken = default);
    }
}
