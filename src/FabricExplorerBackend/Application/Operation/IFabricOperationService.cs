using FabricExplorerBackend.Application.Operation.Contracts;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Operation
{
    public interface IFabricOperationService
    {
        Task<Result<FabricOperationResponse>> CreateFabricOperationAsync(CreateFabricOperationRequest request, CancellationToken cancellationToken = default);
        Task<Result<FabricOperationResponse>> GetFabricOperationByIdAsync(Guid connectionId, Guid id, CancellationToken ct = default);
    }
}
