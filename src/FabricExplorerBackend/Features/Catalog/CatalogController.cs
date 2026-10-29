using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Features.Catalog.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Features.Catalog
{
    [Route("api/workspaces/{workspaceId:guid}/lakehouses/{lakehouseId:guid}/catalog")]
    [ApiController]
    public class CatalogController(ICatalogService catalogService) : ControllerBase
    {
        /// <summary>Schemas of the lakehouse, each with its tables (names only).</summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<CatalogTreeResponse>>> GetCatalogTree(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid lakehouseId,
            CancellationToken cancellationToken)
            => (await catalogService.GetCatalogTreeAsync(
                connectionId, workspaceId, lakehouseId, Request.ShouldBypassCache(), cancellationToken)).ToActionResult(this);

        /// <summary>Columns (with descriptions when available) of one table.</summary>
        [HttpGet("schemas/{schemaName}/tables/{tableName}")]
        public async Task<ActionResult<ApiResponse<CatalogTableDetailResponse>>> GetTableDetail(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid lakehouseId,
            string schemaName,
            string tableName,
            CancellationToken cancellationToken)
            => (await catalogService.GetTableDetailAsync(
                connectionId, workspaceId, lakehouseId, schemaName, tableName, Request.ShouldBypassCache(), cancellationToken)).ToActionResult(this);
    }
}
