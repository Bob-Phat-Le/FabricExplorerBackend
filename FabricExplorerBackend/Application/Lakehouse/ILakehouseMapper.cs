using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Application.Lakehouse
{
    public interface ILakehouseMapper
    {
        TableResponse Map(FabricTableResponse fabricTableResponse);
        LoadTableRequest Map(ApplicationLoadTableRequest request);
    }
}
