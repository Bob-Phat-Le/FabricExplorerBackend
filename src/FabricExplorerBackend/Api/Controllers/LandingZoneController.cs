using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.LandingZone;
using FabricExplorerBackend.Application.LandingZone.Contracts.Explorers;
using FabricExplorerBackend.Commons.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    /// <summary>
    /// Landing Zone Explorer. Every endpoint works on the Landing Zone configured for the connection
    /// in the X-Connection-Id header; paths are relative to the Landing Zone root.
    /// </summary>
    [Route("api/landing-zone")]
    [ApiController]
    public partial class LandingZoneController(ILandingZoneService landingZoneService) : ControllerBase
    {
        private const long MaxUploadBytes = 2L * 1024 * 1024 * 1024;

        // ---------- configuration ----------

        [HttpGet]
        public async Task<ActionResult<ApiResponse<LandingZoneConfigResponse>>> GetConfig(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            CancellationToken cancellationToken)
            => (await landingZoneService.GetConfigAsync(connectionId, cancellationToken)).ToActionResult(this);

        [HttpPut]
        public async Task<ActionResult<ApiResponse<LandingZoneConfigResponse>>> Configure(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromBody] ConfigureLandingZoneRequest request,
            CancellationToken cancellationToken)
            => (await landingZoneService.ConfigureAsync(connectionId, request, cancellationToken)).ToActionResult(this);

        [HttpDelete]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> RemoveConfig(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            CancellationToken cancellationToken)
            => (await landingZoneService.RemoveConfigAsync(connectionId, cancellationToken)).ToActionResult(this);

        // ---------- browsing ----------

        /// <summary>Files and folders directly inside a folder.</summary>
        [HttpGet("items")]
        public async Task<ActionResult<ApiResponse<LandingZoneListResponse>>> ListItems(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromQuery] string? path,
            CancellationToken cancellationToken)
            => (await landingZoneService.ListItemsAsync(connectionId, path, cancellationToken)).ToActionResult(this);

        /// <summary>Sub-folders directly inside a folder (lazy loading of the folder tree).</summary>
        [HttpGet("folders")]
        public async Task<ActionResult<ApiResponse<LandingZoneFoldersResponse>>> ListFolders(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromQuery] string? path,
            CancellationToken cancellationToken)
            => (await landingZoneService.ListFoldersAsync(connectionId, path, cancellationToken)).ToActionResult(this);

        /// <summary>Recursive search by name below a folder (default: the whole Landing Zone).</summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<LandingZoneListResponse>>> Search(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromQuery] string? path,
            [FromQuery] string? query,
            CancellationToken cancellationToken)
            => (await landingZoneService.SearchAsync(connectionId, path, query, cancellationToken)).ToActionResult(this);

        [HttpGet("files/details")]
        public async Task<ActionResult<ApiResponse<LandingZoneFileDetailsResponse>>> GetFileDetails(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromQuery] string? path,
            CancellationToken cancellationToken)
            => (await landingZoneService.GetFileDetailsAsync(connectionId, path, cancellationToken)).ToActionResult(this);

        [HttpGet("files/preview")]
        public async Task<ActionResult<ApiResponse<LandingZonePreviewResponse>>> GetPreview(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromQuery] string? path,
            CancellationToken cancellationToken)
            => (await landingZoneService.GetPreviewAsync(connectionId, path, cancellationToken)).ToActionResult(this);

        // ---------- download / upload ----------

        [HttpGet("files/download")]
        public async Task<IActionResult> Download(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromQuery] string? path,
            CancellationToken cancellationToken)
        {
            var result = await landingZoneService.DownloadFileAsync(connectionId, path, cancellationToken);
            if (!result.IsSuccess)
                return result.ToActionResult(this).Result!;

            var file = result.Data!;
            return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
        }

        /// <summary>Several files as one zip.</summary>
        [HttpPost("files/archive")]
        public async Task<IActionResult> DownloadArchive(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromBody] DownloadArchiveRequest request,
            CancellationToken cancellationToken)
        {
            var result = await landingZoneService.DownloadArchiveAsync(connectionId, request, cancellationToken);
            if (!result.IsSuccess)
                return result.ToActionResult(this).Result!;

            var file = result.Data!;
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>Which of the given file names already exist in the destination folder (checked before an upload).</summary>
        [HttpPost("files/exists")]
        public async Task<ActionResult<ApiResponse<CheckFilesExistResponse>>> CheckFilesExist(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromBody] CheckFilesExistRequest request,
            CancellationToken cancellationToken)
            => (await landingZoneService.CheckItemExistAsync(connectionId, request, cancellationToken)).ToActionResult(this);

        [HttpPost("files/upload")]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        public async Task<ActionResult<ApiResponse<LandingZoneBatchResponse>>> Upload(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromForm] UploadLandingZoneFilesRequest request,
            CancellationToken cancellationToken)
        {
            var files = request.Files
                .Select(f => new LandingZoneUploadFile(f.FileName, f.Length, f.OpenReadStream))
                .ToList();

            return (await landingZoneService.UploadAsync(
                connectionId, request.Path, request.Overwrite, files, cancellationToken)).ToActionResult(this);
        }

        // ---------- organize ----------

        [HttpPost("folders")]
        public async Task<ActionResult<ApiResponse<LandingZoneItemResponse>>> CreateFolder(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromBody] CreateFolderRequest request,
            CancellationToken cancellationToken)
            => (await landingZoneService.CreateFolderAsync(connectionId, request, cancellationToken)).ToActionResult(this);

        [HttpPost("items/rename")]
        public async Task<ActionResult<ApiResponse<LandingZoneItemResponse>>> Rename(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromBody] RenameItemRequest request,
            CancellationToken cancellationToken)
            => (await landingZoneService.RenameAsync(connectionId, request, cancellationToken)).ToActionResult(this);

        [HttpPost("items/move")]
        public async Task<ActionResult<ApiResponse<LandingZoneBatchResponse>>> Move(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromBody] TransferItemsRequest request,
            CancellationToken cancellationToken)
            => (await landingZoneService.MoveAsync(connectionId, request, cancellationToken)).ToActionResult(this);

        [HttpPost("items/copy")]
        public async Task<ActionResult<ApiResponse<LandingZoneBatchResponse>>> Copy(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromBody] TransferItemsRequest request,
            CancellationToken cancellationToken)
            => (await landingZoneService.CopyAsync(connectionId, request, cancellationToken)).ToActionResult(this);

        /// <summary>POST instead of DELETE because the items are sent in the body.</summary>
        [HttpPost("items/delete")]
        public async Task<ActionResult<ApiResponse<LandingZoneBatchResponse>>> Delete(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromBody] DeleteItemsRequest request,
            CancellationToken cancellationToken)
            => (await landingZoneService.DeleteAsync(connectionId, request, cancellationToken)).ToActionResult(this);

        // ---------- history ----------

        [HttpGet("operations")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<LandingZoneOperationResponse>>>> GetOperations(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromQuery] int limit = 50,
            CancellationToken cancellationToken = default)
            => (await landingZoneService.GetOperationsAsync(connectionId, limit, cancellationToken)).ToActionResult(this);
    }
}
