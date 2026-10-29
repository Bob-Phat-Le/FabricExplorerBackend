using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Features.Operation.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Features.Operation
{
    [Route("api/[controller]")]
    [ApiController]
    public class OperationsController(IFabricOperationService operationService) : ControllerBase
    {
        [HttpGet("{operationId:guid}", Name = "GetOperation")]
        public async Task<ActionResult<ApiResponse<FabricOperationResponse>>> GetOperation(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid operationId,
            CancellationToken cancellationToken)
            => (await operationService.GetFabricOperationByIdAsync(connectionId, operationId, cancellationToken))
                .ToActionResult(this);
    }
}
