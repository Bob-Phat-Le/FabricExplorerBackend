using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;

namespace FabricExplorerBackend.Application.Lakehouse
{
    public interface ILakehouseMapper
    {
        TableResponse Map(FabricTableResponse fabricTableResponse);
    }
}
