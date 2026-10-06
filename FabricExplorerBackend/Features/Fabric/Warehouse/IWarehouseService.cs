using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.Warehouse;

namespace FabricExplorerBackend.Features.Fabric.Warehouse
{
    public interface IWarehouseService
    {
        Task<Result<IEnumerable<WarehouseResponse>>> GetAllWarehouseByWorkspaceIdAsync(Guid workspaceId);

    }
}
