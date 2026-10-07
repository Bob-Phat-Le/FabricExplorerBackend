using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation;
using FabricExplorerBackend.Features.Fabric.Operation;
using FabricExplorerBackend.Features.Result;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OperationsController(IFabricOperationService fabricOperationService) : ControllerBase
    {
        [HttpGet("{operationId:guid}")]
        public async Task<ActionResult<ApiResponse<FabricOperationResponse>>> GetOperation(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid operationId,
            CancellationToken cancellationToken)
        {
            var response = await fabricOperationService.GetFabricOperationAsync(connectionId, operationId, cancellationToken);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<FabricOperationResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<FabricOperationResponse>(statusCode, response.Errors));
        }
    }
}
