using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Features.Workspace.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Features.Workspace
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkspacesController(IWorkspaceService workspaceService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<WorkspaceListItemResponse>>>> GetAllWorkspaces(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            CancellationToken cancellationToken)
            => (await workspaceService.GetAllWorkspacesAsync(connectionId, Request.ShouldBypassCache(), cancellationToken)).ToActionResult(this);
    }
}
