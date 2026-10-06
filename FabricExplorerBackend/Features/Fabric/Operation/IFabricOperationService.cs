using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.FabricOperation;
using FabricExplorerBackend.Commons.Models.Responses.Fabric;

namespace FabricExplorerBackend.Features.Fabric.Operation
{
    public interface IFabricOperationService
    {
        Task<ApiResponse<ConfirmationResponse>> CreateFabricOperationAsync(CreateFabricOperationRequest request);
    }
}
