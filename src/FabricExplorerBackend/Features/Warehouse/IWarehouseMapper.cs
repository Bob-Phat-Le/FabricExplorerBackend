using FabricExplorerBackend.Features.Warehouse.Contracts;

namespace FabricExplorerBackend.Features.Warehouse
{
    public interface IWarehouseMapper
    {
        WarehouseMinimalResponse Map(Microsoft.Fabric.Api.Warehouse.Models.Warehouse warehouse);
        IEnumerable<WarehouseMinimalResponse> Map(IEnumerable<Microsoft.Fabric.Api.Warehouse.Models.Warehouse> warehouses);
    }
}
