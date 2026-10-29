using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Features.MirroredDatabase.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Features.MirroredDatabase
{
    [Route("/api/workspaces/{workspaceId}/[controller]")]
    [ApiController]
    public class MirroredDatabasesController(IMirroredDatabaseService mirroredDatabaseService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<MirroredDatabaseListItemResponse>>>> GetAllMirroredDatabases(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            CancellationToken cancellationToken)
            => (await mirroredDatabaseService.ListMirroredDatabasesAsync(connectionId, workspaceId, Request.ShouldBypassCache(), cancellationToken)).ToActionResult(this);

        [HttpGet("{mirroredDatabaseId:guid}")]
        public async Task<ActionResult<ApiResponse<EventStreamingAdapter<MirroredDatabaseDetailResponse>>>> GetMirroredDatabase(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId,
            CancellationToken cancellationToken)
            => (await mirroredDatabaseService.GetMirroredDatabaseByIdAsync(connectionId, workspaceId, mirroredDatabaseId, cancellationToken)).ToActionResult(this);

        [HttpGet("{mirroredDatabaseId:guid}/status")]
        public async Task<ActionResult<ApiResponse<EventStreamingAdapter<MirroringDatabaseStatusResponse>>>> GetMirroringStatus(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId,
            CancellationToken cancellationToken)
            => (await mirroredDatabaseService.GetMirroringStatusAsync(connectionId, workspaceId, mirroredDatabaseId, cancellationToken)).ToActionResult(this);

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
