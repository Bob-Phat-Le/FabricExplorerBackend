using FabricExplorerBackend.Features.Operation.Contracts;

namespace FabricExplorerBackend.Features.Operation
{
    public interface IFabricOperationMapper
    {
        CreateFabricOperationRequest Map(Azure.Response response);
        CreateFabricOperationRequest Map(HttpResponseMessage http);
        Domain.Entities.FabricOperation Map(CreateFabricOperationRequest request);
        FabricOperationResponse Map(Domain.Entities.FabricOperation operation);
    }
}
