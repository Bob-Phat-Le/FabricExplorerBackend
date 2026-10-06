using FabricExplorerBackend.Commons.Models.Requests.FabricOperation;

namespace FabricExplorerBackend.Features.Fabric.Operation
{
    public interface IFabricOperationMapper
    {
        CreateFabricOperationRequest Map(Azure.Response response);
        Domain.Entities.FabricOperation Map(CreateFabricOperationRequest request);
    }
}
