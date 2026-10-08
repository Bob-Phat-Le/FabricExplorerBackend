using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Workspace;
using FabricExplorerBackend.Features.Fabric.Workspace;
using FabricExplorerBackend.Features.Result;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkspacesController(IWorkspaceService workspaceService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<WorkspaceListItemResponse>>>> GetAllWorkspaces(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId)
        {
            var response = await workspaceService.GetAllWorkspacesAsync(connectionId);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<IEnumerable<WorkspaceListItemResponse>>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<IEnumerable<WorkspaceListItemResponse>>(statusCode, response.Errors));
        }
    }
}
