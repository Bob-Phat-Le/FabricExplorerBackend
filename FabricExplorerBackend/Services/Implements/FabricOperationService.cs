using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Mappers.FabricOperation;
using FabricExplorerBackend.Models.Requests.FabricOperation;
using FabricExplorerBackend.Models.Responses;
using FabricExplorerBackend.Repositories.Interfaces;
using FabricExplorerBackend.Services.Interfaces;

namespace FabricExplorerBackend.Services.Implements
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
