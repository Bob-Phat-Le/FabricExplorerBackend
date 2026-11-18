using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.MirroredDatabase;
using FabricExplorerBackend.Application.MirroredDatabase.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.EventStreaming;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
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

        [HttpGet("{mirroredDatabaseId:guid}/landing-zones/{*path}")]
        [HttpGet("{mirroredDatabaseId:guid}/landing-zones")]
        public async Task<ActionResult<ApiResponse<LandingZoneListResponse>>> ListObjectsInLandingZoneAsync(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId,
            string? path,
            [FromQuery] LandingZoneRequestQuery query,
            CancellationToken cancellationToken)
            => (await mirroredDatabaseService.ListObjectsInLandingZoneAsync(connectionId, workspaceId, mirroredDatabaseId, path, query, cancellationToken)).ToActionResult(this);

        [HttpPut("{mirroredDatabaseId:guid}/landing-zones/{*path}")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> CreateFolderAsync([FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId,
            string? path,
            CancellationToken cancellationToken)
            => (await mirroredDatabaseService.CreateFolderAsync(connectionId, workspaceId, mirroredDatabaseId, path, cancellationToken)).ToActionResult(this);

        [HttpDelete("{mirroredDatabaseId:guid}/landing-zones/{*path}")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> DeleteFolderAsync([FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId,
            string? path,
            CancellationToken cancellationToken)
            => (await mirroredDatabaseService.DeleteFolderAsync(connectionId, workspaceId, mirroredDatabaseId, path, cancellationToken)).ToActionResult(this);
    }
}
