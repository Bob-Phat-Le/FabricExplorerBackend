using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Features.Operation.Contracts;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Features.Operation
{
    public class FabricOperationService(
        IUnitOfWork unitOfWork,
        IFabricOperationMapper mapper) : IFabricOperationService
    {
        public async Task<Result<FabricOperationResponse>> CreateFabricOperationAsync(
            CreateFabricOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            var operation = mapper.Map(request);
            await unitOfWork.FabricOperationRepository.AddAsync(operation);

            if (await unitOfWork.SaveChangesAsync() <= 0)
                throw new InvalidOperationException(
                    $"Saving fabric operation {operation.FabricOperationId} affected 0 rows.");

            return Result<FabricOperationResponse>.Success(mapper.Map(operation));
        }

        public async Task<Result<FabricOperationResponse>> GetFabricOperationByIdAsync(
            Guid connectionId, Guid id, CancellationToken ct = default)
        {
            var operation = await unitOfWork.FabricOperationRepository.GetByIdAsync(id, cancellationToken: ct);

            if (operation is null || operation.ConnectionId != connectionId)
                return Error.NotFound(ErrorCodes.Operation.NotFound, "Operation not found.");

            return Result<FabricOperationResponse>.Success(mapper.Map(operation));
        }
    }
}
