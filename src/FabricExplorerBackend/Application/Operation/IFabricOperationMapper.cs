using FabricExplorerBackend.Application.Operation.Contracts;

namespace FabricExplorerBackend.Application.Operation
{
    public interface IFabricOperationMapper
    {
        CreateFabricOperationRequest Map(Azure.Response response);
        Domain.Entities.FabricOperation Map(CreateFabricOperationRequest request);
        FabricOperationResponse Map(Domain.Entities.FabricOperation operation);
    }
}
