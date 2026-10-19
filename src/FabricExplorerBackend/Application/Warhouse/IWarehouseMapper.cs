using FabricExplorerBackend.Application.Warhouse.Contracts;

namespace FabricExplorerBackend.Application.Warhouse
{
    public interface IWarehouseMapper
    {
        WarehouseMinimalResponse Map(Microsoft.Fabric.Api.Warehouse.Models.Warehouse warehouse);
        IEnumerable<WarehouseMinimalResponse> Map(IEnumerable<Microsoft.Fabric.Api.Warehouse.Models.Warehouse> warehouses);
    }
}
