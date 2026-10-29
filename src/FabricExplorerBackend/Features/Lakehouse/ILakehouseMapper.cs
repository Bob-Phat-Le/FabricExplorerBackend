using FabricExplorerBackend.Features.Lakehouse.Contracts;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Features.Lakehouse
{
    public interface ILakehouseMapper
    {
        TableResponse Map(FabricTableResponse fabricTableResponse);
        LoadTableRequest Map(ApplicationLoadTableRequest request);
    }
}
