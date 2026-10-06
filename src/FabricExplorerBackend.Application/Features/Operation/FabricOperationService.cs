using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.FabricOperation;
using FabricExplorerBackend.Commons.Models.Responses;
using FabricExplorerBackend.Features.Mappers.FabricOperation;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Features.Fabric.Operation
{
    public class FabricOperationService(
        IUnitOfWork unitOfWork,
        IFabricOperationMapper mapper) : IFabricOperationService
    {
        public async Task<ApiResponse<ConfirmationResponse>> CreateFabricOperationAsync(CreateFabricOperationRequest request)
        {
            var operation = mapper.Map(request);
            await unitOfWork.FabricOperationRepository.AddAsync(operation);
            var lineChanges = await unitOfWork.SaveChangesAsync();

            if (lineChanges > 0)
                return new(StatusCodes.Status200OK, new ConfirmationResponse { Message = "Fabric operation created successfully." });
            return new(StatusCodes.Status400BadRequest, "Failed to create fabric operation.");

            throw new NotImplementedException();
        }
    }
}
