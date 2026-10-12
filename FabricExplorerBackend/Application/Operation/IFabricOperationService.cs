using FabricExplorerBackend.Application.Operation.Contracts;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Operation
{
    public interface IFabricOperationService
    {
        Task<Result<FabricOperationResponse>> CreateFabricOperationAsync(CreateFabricOperationRequest request);
        Task<Result<FabricOperationResponse>> GetFabricOperationByIdAsync(Guid id);
    }
}
