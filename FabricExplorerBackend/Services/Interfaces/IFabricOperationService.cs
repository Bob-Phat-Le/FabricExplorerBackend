using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Models.Requests.FabricOperation;
using FabricExplorerBackend.Models.Responses;

namespace FabricExplorerBackend.Services.Interfaces
{
    public interface IFabricOperationService
    {
        Task<ApiResponse<ConfirmationResponse>> CreateFabricOperationAsync(CreateFabricOperationRequest request);
    }
}
