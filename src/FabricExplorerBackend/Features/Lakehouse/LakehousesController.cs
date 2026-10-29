using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Features.Lakehouse.Contracts;
using FabricExplorerBackend.Features.Operation.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Features.Lakehouse
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
        public async Task<ActionResult<ApiResponse<EventStreamingAdapter<PagedResponseWithCursor<IEnumerable<TableResponse>>>>>> GetAllTables(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid lakehouseId,
            [FromQuery] PaginationWithCursorRequest? paginationRequest,
            CancellationToken cancellationToken)
            => (await lakehouseService.GetAllTablesAsync(
                connectionId, workspaceId, lakehouseId, paginationRequest, Request.ShouldBypassCache(), cancellationToken)).ToActionResult(this);

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

            if (data.OperationId is not { } operationId)
                return Ok(new ApiResponse<OperationAcceptedResponse> { StatusCode = 200, Data = data });

            Response.Headers.Location = Url.RouteUrl("GetOperation", new { operationId });
            return StatusCode(StatusCodes.Status202Accepted,
                new ApiResponse<OperationAcceptedResponse> { StatusCode = 202, Data = data });
        }
    }
}
