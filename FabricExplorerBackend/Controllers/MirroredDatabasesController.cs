using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase;
using FabricExplorerBackend.Features.Fabric.MirroredDatabase;
using FabricExplorerBackend.Features.Result;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Controllers
{
    [Route("/api/workspaces/{workspaceId}/[controller]")]
    [ApiController]
    public class MirroredDatabasesController(IMirroredDatabaseService mirroredDatabaseService) : ControllerBase
    {
        // Danh sách mirrored database của workspace: tên, nguồn, ngày tạo, Active/Offline, workspace
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<MirroredDatabaseDetailResponse>>>> GetAllMirroredDatabases(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId)
        {
            var response = await mirroredDatabaseService.ListMirroredDatabasesAsync(connectionId, workspaceId);
            return ToActionResult(response);
        }

        [HttpGet("{mirroredDatabaseId:guid}")]
        public async Task<ActionResult<ApiResponse<MirroredDatabaseDetailResponse>>> GetMirroredDatabase(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId)
        {
            var response = await mirroredDatabaseService.GetMirroredDatabaseByIdAsync(connectionId, workspaceId, mirroredDatabaseId);
            return ToActionResult(response);
        }

        // Last synchronization, records synchronized, current latency, status
        [HttpGet("{mirroredDatabaseId:guid}/status")]
        public async Task<ActionResult<ApiResponse<MirroringDatabaseStatusResponse>>> GetMirroringStatus(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId)
        {
            var response = await mirroredDatabaseService.GetMirroringStatusAsync(connectionId, workspaceId, mirroredDatabaseId);
            return ToActionResult(response);
        }

        // Table, source, target, status, last sync, lag
        [HttpGet("{mirroredDatabaseId:guid}/tables/status")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TableMirroringStatusResponse>>>> GetTablesMirroringStatus(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId)
        {
            var response = await mirroredDatabaseService.ListTablesMirroringStatusAsync(connectionId, workspaceId, mirroredDatabaseId);
            return ToActionResult(response);
        }

        [HttpPost("{mirroredDatabaseId:guid}/start")]
        public async Task<ActionResult<ApiResponse<MirroringActionResponse>>> StartMirroring(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId)
        {
            var response = await mirroredDatabaseService.StartMirroringAsync(connectionId, workspaceId, mirroredDatabaseId);
            return ToActionResult(response);
        }

        [HttpPost("{mirroredDatabaseId:guid}/stop")]
        public async Task<ActionResult<ApiResponse<MirroringActionResponse>>> StopMirroring(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId)
        {
            var response = await mirroredDatabaseService.StopMirroringAsync(connectionId, workspaceId, mirroredDatabaseId);
            return ToActionResult(response);
        }

        private ActionResult<ApiResponse<T>> ToActionResult<T>(Result<T> result)
        {
            var statusCode = ResultMapper.ToHttpStatusCode(result.ResultStatus);
            if (result.IsSuccess)
                return Ok(new ApiResponse<T>(statusCode, result.Data));
            return StatusCode(statusCode, new ApiResponse<T>(statusCode, result.Errors));
        }
    }
}
