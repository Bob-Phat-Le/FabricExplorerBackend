using FabricExplorerBackend.Application.Warhouse;
using FabricExplorerBackend.Application.Warhouse.Contracts;
using Riok.Mapperly.Abstractions;

namespace FabricExplorerBackend.Features.Fabric.Warehouse
{
    [Mapper]
    public partial class WarehouseMapper : IWarehouseMapper
    {
        public partial WarehouseMinimalResponse Map(Microsoft.Fabric.Api.Warehouse.Models.Warehouse warehouse);

        public partial IEnumerable<WarehouseMinimalResponse> Map(IEnumerable<Microsoft.Fabric.Api.Warehouse.Models.Warehouse> warehouses);
    }
}
