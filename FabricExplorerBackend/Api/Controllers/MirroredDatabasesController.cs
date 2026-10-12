using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.MirroredDatabase;
using FabricExplorerBackend.Application.MirroredDatabase.Contracts;
using FabricExplorerBackend.Commons.Results;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    [Route("/api/workspaces/{workspaceId}/[controller]")]
    [ApiController]
    public class MirroredDatabasesController(IMirroredDatabaseService mirroredDatabaseService) : ControllerBase
    {
        // Danh sách nhẹ cho dropdown: id, tên, workspace. Cache-Control: no-cache để lấy mới (nút Reload)
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<MirroredDatabaseListItemResponse>>>> GetAllMirroredDatabases(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            CancellationToken cancellationToken)
            => (await mirroredDatabaseService.ListMirroredDatabasesAsync(connectionId, workspaceId, Request.ShouldBypassCache(), cancellationToken)).ToActionResult(this);

        // Tên, workspace, nguồn, ngày tạo, Active/Offline
        [HttpGet("{mirroredDatabaseId:guid}")]
        public async Task<ActionResult<ApiResponse<MirroredDatabaseDetailResponse>>> GetMirroredDatabase(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId,
            CancellationToken cancellationToken)
            => (await mirroredDatabaseService.GetMirroredDatabaseByIdAsync(connectionId, workspaceId, mirroredDatabaseId, cancellationToken)).ToActionResult(this);

        // Last synchronization, records synchronized, current latency, status
        [HttpGet("{mirroredDatabaseId:guid}/status")]
        public async Task<ActionResult<ApiResponse<MirroringDatabaseStatusResponse>>> GetMirroringStatus(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId,
            CancellationToken cancellationToken)
            => (await mirroredDatabaseService.GetMirroringStatusAsync(connectionId, workspaceId, mirroredDatabaseId, cancellationToken)).ToActionResult(this);

        // Table, source, target, status, last sync, lag
        [HttpGet("{mirroredDatabaseId:guid}/tables/status")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TableMirroringStatusResponse>>>> GetTablesMirroringStatus(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId,
            CancellationToken cancellationToken)
            => (await mirroredDatabaseService.ListTablesMirroringStatusAsync(connectionId, workspaceId, mirroredDatabaseId, cancellationToken)).ToActionResult(this);

        [HttpPost("{mirroredDatabaseId:guid}/start")]
        public async Task<ActionResult<ApiResponse<MirroringActionResponse>>> StartMirroring(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId,
            CancellationToken cancellationToken)
            => (await mirroredDatabaseService.StartMirroringAsync(connectionId, workspaceId, mirroredDatabaseId, cancellationToken)).ToActionResult(this);

        [HttpPost("{mirroredDatabaseId:guid}/stop")]
        public async Task<ActionResult<ApiResponse<MirroringActionResponse>>> StopMirroring(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId,
            CancellationToken cancellationToken)
            => (await mirroredDatabaseService.StopMirroringAsync(connectionId, workspaceId, mirroredDatabaseId, cancellationToken)).ToActionResult(this);
    }
}
