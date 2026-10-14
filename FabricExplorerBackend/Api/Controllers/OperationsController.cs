using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Application.Operation.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OperationsController(IFabricOperationService operationService) : ControllerBase
    {
        // Theo dõi trạng thái một Fabric LRO (do worker cập nhật) bằng OperationId mà API start/stop trả về
        [HttpGet("{operationId:guid}", Name = "GetOperation")]
        public async Task<ActionResult<ApiResponse<FabricOperationResponse>>> GetOperation(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid operationId,
            CancellationToken cancellationToken)
            => (await operationService.GetFabricOperationByIdAsync(connectionId, operationId, cancellationToken))
                .ToActionResult(this);
    }
}
