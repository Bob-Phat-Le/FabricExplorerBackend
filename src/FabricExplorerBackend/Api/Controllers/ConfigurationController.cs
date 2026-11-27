using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.Configuration;
using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Commons.Paginations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    /// <summary>Import and export of configuration: connections and saved queries.</summary>
    [EnableCors("MyAllowSpecificOrigin")]
    [Authorize]
    [Route("api/configuration")]
    [ApiController]
    public class ConfigurationController(
        IConfigurationCatalogService catalogService,
        IConfigurationExportService exportService,
        IConfigurationImportService importService,
        IConfigurationHistoryService historyService) : ControllerBase
    {
        private const int MaxBodyBytes = 6 * 1024 * 1024;

        [HttpGet("capabilities")]
        public ActionResult<ApiResponse<ConfigurationCapabilitiesResponse>> GetCapabilities()
            => catalogService.GetCapabilities().ToActionResult(this);

        /// <summary>Connections and saved queries as one list, filtered and paged on the server.</summary>
        [HttpGet("items")]
        public async Task<ActionResult<ApiResponse<PagedResponse<IReadOnlyList<ConfigurationItemResponse>>>>> GetItems(
            [FromQuery] ConfigurationItemsQuery query, CancellationToken cancellationToken)
            => (await catalogService.GetItemsAsync(query, cancellationToken)).ToActionResult(this);

        /// <summary>Returns the configuration file (application/json, attachment).</summary>
        [HttpPost("export")]
        public async Task<IActionResult> Export([FromBody] ExportRequest request, CancellationToken cancellationToken)
        {
            var result = await exportService.ExportAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return result.ToActionResult(this).Result!;

            var file = result.Data!;
            Response.Headers["X-Configuration-Exported"] = file.Exported.ToString();
            Response.Headers["X-Configuration-Skipped"] = file.Skipped.ToString();
            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpPost("import/preview")]
        [RequestSizeLimit(MaxBodyBytes)]
        public async Task<ActionResult<ApiResponse<PreviewResponse>>> Preview(
            [FromBody] PreviewRequest request, CancellationToken cancellationToken)
            => (await importService.PreviewAsync(request, cancellationToken)).ToActionResult(this);

        [HttpPost("import")]
        [RequestSizeLimit(MaxBodyBytes)]
        public async Task<ActionResult<ApiResponse<ImportResponse>>> Import(
            [FromBody] ImportRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken cancellationToken)
            => (await importService.ImportAsync(request, idempotencyKey, cancellationToken)).ToActionResult(this);

        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ConfigurationHistoryItem>>>> GetHistory(
            [FromQuery] int? limit, CancellationToken cancellationToken)
            => (await historyService.GetRecentAsync(limit, cancellationToken)).ToActionResult(this);
    }
}
