using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.Connection;
using FabricExplorerBackend.Application.Connection.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Paginations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    [EnableCors("MyAllowSpecificOrigin")]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectionsController(IConnectionService connectionService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<IEnumerable<ConnectionResponse>>>>> GetAllConnections([FromQuery] PaginationRequest? paginationRequest, CancellationToken cancellationToken = default)
            => (await connectionService.GetAllConnectionsAsync(paginationRequest)).ToActionResult(this);

        [HttpGet("{connectionId}")]
        public async Task<ActionResult<ApiResponse<ConnectionResponse>>> GetConnectionById(Guid connectionId)
            => (await connectionService.GetConnectionByIdAsync(connectionId)).ToActionResult(this);

        [HttpGet("active")]
        public async Task<ActionResult<ApiResponse<ConnectionResponse>>> GetActiveConnection(CancellationToken cancellationToken = default)
            => (await connectionService.GetActiveConnectionAsync()).ToActionResult(this);

        [HttpPatch("{connectionId}")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> UpdateConnection(Guid connectionId, [FromBody] UpdateConnectionRequest request, CancellationToken cancellationToken = default)
            => (await connectionService.UpdateConnection(connectionId, request)).ToActionResult(this);

        [HttpDelete("{connectionId}")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> DeleteConnection(Guid connectionId, CancellationToken cancellationToken = default)
            => (await connectionService.DeleteConnection(connectionId)).ToActionResult(this);

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> CreateConnection(CreateConnectionRequest request, CancellationToken cancellationToken = default)
            => (await connectionService.CreateConnectionAsync(request)).ToActionResult(this);

        [HttpPost("{connectionId}/test")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> TestConnection(Guid connectionId, CancellationToken cancellationToken = default)
            => (await connectionService.TestConnection(connectionId)).ToActionResult(this);

        [HttpPost("{connectionId}/active")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> ActiveConnection(Guid connectionId, CancellationToken cancellationToken = default)
            => (await connectionService.ActiveConnection(connectionId)).ToActionResult(this);
    }
}
