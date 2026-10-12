using FabricExplorerBackend.Application.Operation.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Application.Operation
{
    public class FabricOperationService(
        IUnitOfWork unitOfWork,
        IFabricOperationMapper mapper) : IFabricOperationService
    {
        public async Task<Result<FabricOperationResponse>> CreateFabricOperationAsync(CreateFabricOperationRequest request, CancellationToken cancellationToken = default)
        {
            var operation = mapper.Map(request);
            await unitOfWork.FabricOperationRepository.AddAsync(operation);
            var lineChanges = await unitOfWork.SaveChangesAsync();

            if (lineChanges <= 0)
                throw new InvalidOperationException($"Failed create operation");

            return Result<FabricOperationResponse>.Success(mapper.Map(operation));
        }

        public async Task<Result<FabricOperationResponse>> GetFabricOperationByIdAsync(Guid id, CancellationToken cancellation = default)
        {
            var operation = await unitOfWork.FabricOperationRepository.GetByIdAsync(id);
            if (operation == null)
                return Error.NotFound(ErrorCodes.Operation.NotFound, "Operation not found");

            return Result<FabricOperationResponse>.Success(mapper.Map(operation));
        }
    }
}
