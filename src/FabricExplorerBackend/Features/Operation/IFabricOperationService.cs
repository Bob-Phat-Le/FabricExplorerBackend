using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Features.Operation.Contracts;

namespace FabricExplorerBackend.Features.Operation
{
    public interface IFabricOperationService
    {
        Task<Result<FabricOperationResponse>> CreateFabricOperationAsync(CreateFabricOperationRequest request, CancellationToken cancellationToken = default);
        Task<Result<FabricOperationResponse>> GetFabricOperationByIdAsync(Guid connectionId, Guid id, CancellationToken ct = default);
    }
}
