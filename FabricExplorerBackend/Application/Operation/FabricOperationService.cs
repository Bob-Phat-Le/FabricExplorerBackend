using FabricExplorerBackend.Application.Operation.Contracts;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Application.Operation
{
    public class FabricOperationService(
        IUnitOfWork unitOfWork,
        IFabricOperationMapper mapper) : IFabricOperationService
    {
        public async Task<Result<FabricOperationResponse>> CreateFabricOperationAsync(CreateFabricOperationRequest request)
        {
            try
            {
                var operation = mapper.Map(request);
                await unitOfWork.FabricOperationRepository.AddAsync(operation);
                var lineChanges = await unitOfWork.SaveChangesAsync();

                if (lineChanges <= 0)
                    return Result<FabricOperationResponse>.Failure(ErrorKind.BadRequest, "Failed to create fabric operation.");

                return Result<FabricOperationResponse>.Success(mapper.Map(operation));
            }
            catch (Exception ex)
            {
                return Result<FabricOperationResponse>.Failure(ErrorKind.InternalError, ex.Message);
            }
        }

        public async Task<Result<FabricOperationResponse>> GetFabricOperationByIdAsync(Guid id)
        {
            try
            {
                var operation = await unitOfWork.FabricOperationRepository.GetByIdAsync(id);
                if (operation == null)
                    return Result<FabricOperationResponse>.Failure(ErrorKind.NotFound, "operation not found");

                return Result<FabricOperationResponse>.Success(mapper.Map(operation));
            }
            catch (Exception ex)
            {
                return Result<FabricOperationResponse>.Failure(ErrorKind.InternalError, ex.Message);
            }
        }
    }
}
