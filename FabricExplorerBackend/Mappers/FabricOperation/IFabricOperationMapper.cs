using FabricExplorerBackend.Models.Requests.FabricOperation;
using FabricExplorerBackend.Entities;

namespace FabricExplorerBackend.Mappers.FabricOperation
{
    public interface IFabricOperationMapper
    {
        CreateFabricOperationRequest Map(Azure.Response response);
        Entities.FabricOperation Map(CreateFabricOperationRequest request);
    }
}
