using FabricExplorerBackend.Commons.Models.Responses.Fabric.Warehouse;

namespace FabricExplorerBackend.Features.Fabric.Warehouse
{
    public interface IWarehouseMapper
    {
        WarehouseMinimalResponse Map(Microsoft.Fabric.Api.Warehouse.Models.Warehouse warehouse);
        IEnumerable<WarehouseMinimalResponse> Map(IEnumerable<Microsoft.Fabric.Api.Warehouse.Models.Warehouse> warehouses);
    }
}
