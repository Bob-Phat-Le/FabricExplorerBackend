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
        {
            var response = await fabricOperationService.GetFabricOperationByIdAsync(operationId);
            var statusCode = ResultExtensions.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<FabricOperationResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<FabricOperationResponse>(statusCode, response.Errors));
        }
    }
}
