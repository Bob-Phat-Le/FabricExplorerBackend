using FabricExplorerBackend.Commons.Models.Requests.Fabric.FabricOperation;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation;

namespace FabricExplorerBackend.Features.Fabric.Operation
{
    public interface IFabricOperationMapper
    {
        CreateFabricOperationRequest Map(Azure.Response response);
        Domain.Entities.FabricOperation Map(CreateFabricOperationRequest request);
        FabricOperationResponse Map(Domain.Entities.FabricOperation operation);
    }
}
