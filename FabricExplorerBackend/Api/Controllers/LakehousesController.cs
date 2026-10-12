using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.Lakehouse;
using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Paginations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Api.Controllers
{
    [Route("api/workspaces/{workspaceId}/[controller]")]
    [ApiController]
    public class LakehousesController(ILakehouseService lakehouseService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<LakehouseListItemResponse>>>> GetAllLakehouses(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            CancellationToken cancellationToken)
            => (await lakehouseService.GetAllLakehousesAsync(connectionId, workspaceId, Request.ShouldBypassCache(), cancellationToken)).ToActionResult(this);

        [HttpGet("{lakehouseId}/tables")]
        public async Task<ActionResult<ApiResponse<PagedResponseWithCursor<IEnumerable<TableResponse>>>>> GetAllTables(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid lakehouseId,
            [FromQuery] PaginationWithCursorRequest? paginationRequest,
            CancellationToken cancellationToken)
            => (await lakehouseService.GetAllTablesAsync(
                connectionId, workspaceId, lakehouseId, paginationRequest, Request.ShouldBypassCache(), cancellationToken)).ToActionResult(this);
            // workspaceId lấy từ route và được truyền xuống: lakehouse ở workspace nào cũng đọc đúng workspace đó

        // UNFINISHED-DO NOT USE THIS ENDPOINT
        [HttpPost("{lakehouseId}/tables/{tableName}/load")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> LoadTable(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid lakehouseId,
            string tableName,
            [FromBody] LoadTableRequest request,
            CancellationToken cancellationToken = default)
            => (await lakehouseService.LoadTableAsync(connectionId, lakehouseId, tableName, request)).ToActionResult(this);
    }
}
