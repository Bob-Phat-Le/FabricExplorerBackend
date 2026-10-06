using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public interface ILakehouseMapper
    {
        TableResponse Map(FabricTableResponse fabricTableResponse);
    }
}
