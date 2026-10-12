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
        {
            var response = await workspaceService.GetAllWorkspacesAsync(connectionId, Request.ShouldBypassCache(), cancellationToken);
            var statusCode = ResultExtensions.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<IEnumerable<WorkspaceListItemResponse>>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<IEnumerable<WorkspaceListItemResponse>>(statusCode, response.Errors));
        }
    }
}
