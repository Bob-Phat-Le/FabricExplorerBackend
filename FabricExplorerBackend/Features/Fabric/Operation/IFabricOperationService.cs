using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.FabricOperation;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation;

namespace FabricExplorerBackend.Features.Fabric.Operation
{
    public interface IFabricOperationService
    {
        Task<Result<FabricOperationResponse>> CreateFabricOperationAsync(CreateFabricOperationRequest request);
        Task<Result<FabricOperationResponse>> GetFabricOperationByIdAsync(Guid id);
    }
}
