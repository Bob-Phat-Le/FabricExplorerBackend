using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.FabricOperation;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Features.Fabric.Operation
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
                    return Result<FabricOperationResponse>.Failure(ResultStatus.BadRequest, "Failed to create fabric operation.");

                return Result<FabricOperationResponse>.Success(mapper.Map(operation));
            }
            catch (Exception ex)
            {
                return Result<FabricOperationResponse>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }

        public async Task<Result<FabricOperationResponse>> GetFabricOperationByIdAsync(Guid id)
        {
            try
            {
                var operation = await unitOfWork.FabricOperationRepository.GetByIdAsync(id);
                if (operation == null)
                    return Result<FabricOperationResponse>.Failure(ResultStatus.NotFound, "operation not found");

                return Result<FabricOperationResponse>.Success(mapper.Map(operation));
            }
            catch (Exception ex)
            {
                return Result<FabricOperationResponse>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }
    }
}
