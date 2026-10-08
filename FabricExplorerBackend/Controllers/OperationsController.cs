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
        // Theo dõi trạng thái một Fabric LRO (do worker cập nhật) bằng OperationId mà API start/stop trả về
        [HttpGet("{operationId:guid}")]
        public async Task<ActionResult<ApiResponse<FabricOperationResponse>>> GetOperation(Guid operationId)
        {
            var response = await fabricOperationService.GetFabricOperationByIdAsync(operationId);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<FabricOperationResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<FabricOperationResponse>(statusCode, response.Errors));
        }
    }
}
