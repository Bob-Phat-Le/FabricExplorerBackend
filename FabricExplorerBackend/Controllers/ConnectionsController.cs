using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Models.Requests.Connection;
using FabricExplorerBackend.Models.Responses;
using FabricExplorerBackend.Models.Responses.Connection;
using FabricExplorerBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectionsController(IConnectionService connectionService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ConnectionResponse>>>> GetAllConnections()
        {
            var response = await connectionService.GetAllConnectionsAsync();
            if (response.IsSuccess)
                return Ok(response);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{connectionId}")]
        public async Task<ActionResult<ApiResponse<ConnectionResponse>>> GetConnectionById(Guid connectionId)
        {
            var response = await connectionService.GetConnectionByIdAsync(connectionId);
            if (response.IsSuccess)
                return Ok(response);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPatch("{connectionId}")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> UpdateConnection(Guid connectionId, [FromBody] UpdateConnectionRequest request)
        {
            var response = await connectionService.UpdateConnection(connectionId, request);
            if (response.IsSuccess)
                return Ok(response);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{connectionId}")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> DeleteConnection(Guid connectionId)
        {
            var response = await connectionService.DeleteConnection(connectionId);
            if (response.IsSuccess)
                return Ok(response);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> CreateConnection(CreateConnectionRequest request)
        {
            var response = await connectionService.CreateConnectionAsync(request);
            if (response.IsSuccess)
                return Ok(response);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("{connectionId}/test")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> TestConnection(Guid connectionId)
        {
            var response = await connectionService.TestConnection(connectionId);
            if (response.IsSuccess)
                return Ok(response);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("{connectionId}/active")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> ActiveConnection(Guid connectionId, [FromHeader(Name = "X-Current-Connection-Id")] Guid currentConnectionId)
        {

        }
    }
}
