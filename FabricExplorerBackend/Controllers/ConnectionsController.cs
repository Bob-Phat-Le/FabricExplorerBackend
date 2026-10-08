using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Connection;
using FabricExplorerBackend.Commons.Models.Requests.Paginations;
using FabricExplorerBackend.Commons.Models.Responses.Connection;
using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Commons.Models.Responses.Paginations;
using FabricExplorerBackend.Features.Connection;
using FabricExplorerBackend.Features.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Controllers
{
    [EnableCors("MyAllowSpecificOrigin")]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectionsController(IConnectionService connectionService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<IEnumerable<ConnectionResponse>>>>> GetAllConnections([FromQuery] PaginationRequest? paginationRequest)
        {
            var response = await connectionService.GetAllConnectionsAsync(paginationRequest);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<PagedResponse<IEnumerable<ConnectionResponse>>>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<PagedResponse<IEnumerable<ConnectionResponse>>>(statusCode, response.Errors));
        }

        [HttpGet("{connectionId}")]
        public async Task<ActionResult<ApiResponse<ConnectionResponse>>> GetConnectionById(Guid connectionId)
        {
            var response = await connectionService.GetConnectionByIdAsync(connectionId);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<ConnectionResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<ConnectionResponse>(statusCode, response.Errors));
        }

        [HttpGet("active")]
        public async Task<ActionResult<ApiResponse<ConnectionResponse>>> GetActiveConnection()
        {
            var response = await connectionService.GetActiveConnectionAsync();
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<ConnectionResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<ConnectionResponse>(statusCode, response.Errors));
        }

        [HttpPatch("{connectionId}")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> UpdateConnection(Guid connectionId, [FromBody] UpdateConnectionRequest request)
        {
            var response = await connectionService.UpdateConnection(connectionId, request);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<ConfirmationResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<ConfirmationResponse>(statusCode, response.Errors));
        }

        [HttpDelete("{connectionId}")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> DeleteConnection(Guid connectionId)
        {
            var response = await connectionService.DeleteConnection(connectionId);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<ConfirmationResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<ConfirmationResponse>(statusCode, response.Errors));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> CreateConnection(CreateConnectionRequest request)
        {
            var response = await connectionService.CreateConnectionAsync(request);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<ConfirmationResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<ConfirmationResponse>(statusCode, response.Errors));
        }

        [HttpPost("{connectionId}/test")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> TestConnection(Guid connectionId)
        {
            var response = await connectionService.TestConnection(connectionId);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<ConfirmationResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<ConfirmationResponse>(statusCode, response.Errors));
        }

        [HttpPost("{connectionId}/active")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> ActiveConnection(Guid connectionId)
        {
            var response = await connectionService.ActiveConnection(connectionId);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<ConfirmationResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<ConfirmationResponse>(statusCode, response.Errors));
        }
    }
}
