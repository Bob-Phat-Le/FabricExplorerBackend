using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Application.Operation.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OperationsController(IFabricOperationService fabricOperationService) : ControllerBase
    {
        // Theo dõi trạng thái một Fabric LRO (do worker cập nhật) bằng OperationId mà API start/stop trả về
        [HttpGet("{operationId:guid}")]
        public async Task<ActionResult<ApiResponse<FabricOperationResponse>>> GetOperation(Guid operationId, CancellationToken cancellationToken = default)
            => (await fabricOperationService.GetFabricOperationByIdAsync(operationId)).ToActionResult(this);
    }
}
