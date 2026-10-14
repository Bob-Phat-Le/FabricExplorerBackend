using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.Lakehouse;
using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Application.Operation.Contracts;
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

        [HttpPost("{lakehouseId}/tables/{tableName}/load")]
        public async Task<ActionResult<ApiResponse<OperationAcceptedResponse>>> LoadTable(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid lakehouseId,
            string tableName,
            [FromBody] ApplicationLoadTableRequest request,
            CancellationToken cancellationToken)
        {
            var result = await lakehouseService.LoadTableAsync(
                connectionId, workspaceId, lakehouseId, tableName, request, cancellationToken);

            if (!result.IsSuccess)
                return result.ToActionResult(this);

            var data = result.Data!;

            // Không có OperationId: Fabric xong ngay, hoặc đã nhận nhưng không theo dõi được. Trả 200.
            if (data.OperationId is not { } operationId)
                return Ok(new ApiResponse<OperationAcceptedResponse> { StatusCode = 200, Data = data });

            // 202 Accepted: Fabric đã nhận việc, kết quả cuối cùng chưa có. Location trỏ tới endpoint theo dõi.
            Response.Headers.Location = Url.RouteUrl("GetOperation", new { operationId });
            return StatusCode(StatusCodes.Status202Accepted,
                new ApiResponse<OperationAcceptedResponse> { StatusCode = 202, Data = data });
        }
    }
}
