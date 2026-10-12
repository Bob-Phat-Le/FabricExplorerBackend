using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.Workspace;
using FabricExplorerBackend.Application.Workspace.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkspacesController(IWorkspaceService workspaceService) : ControllerBase
    {
        // Frontend gọi endpoint này trước, rồi dùng từng workspaceId cho /api/workspaces/{id}/Lakehouses|Warehouses|MirroredDatabases
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<WorkspaceListItemResponse>>>> GetAllWorkspaces(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            CancellationToken cancellationToken)
            => (await workspaceService.GetAllWorkspacesAsync(connectionId, Request.ShouldBypassCache(), cancellationToken)).ToActionResult(this);
    }
}
