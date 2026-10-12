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
        {
            var response = await lakehouseService.GetAllLakehousesAsync(connectionId, workspaceId, Request.ShouldBypassCache(), cancellationToken);
            var statusCode = ResultExtensions.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<IEnumerable<LakehouseListItemResponse>>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<IEnumerable<LakehouseListItemResponse>>(statusCode, response.Errors));
        }

        [HttpGet("{lakehouseId}/tables")]
        public async Task<ActionResult<ApiResponse<PagedResponseWithCursor<IEnumerable<TableResponse>>>>> GetAllTables(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid lakehouseId,
            [FromQuery] PaginationWithCursorRequest? paginationRequest,
            CancellationToken cancellationToken)
        {
            // workspaceId lấy từ route và được truyền xuống: lakehouse ở workspace nào cũng đọc đúng workspace đó
            var response = await lakehouseService.GetAllTablesAsync(
                connectionId, workspaceId, lakehouseId, paginationRequest, Request.ShouldBypassCache(), cancellationToken);
            var statusCode = ResultExtensions.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<PagedResponseWithCursor<IEnumerable<TableResponse>>>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<PagedResponseWithCursor<IEnumerable<TableResponse>>>(statusCode, response.Errors));
        }

        // UNFINISHED-DO NOT USE THIS ENDPOINT
        [HttpPost("{lakehouseId}/tables/{tableName}/load")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> LoadTable(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid lakehouseId,
            string tableName,
            [FromBody] LoadTableRequest request)
        {
            var response = await lakehouseService.LoadTableAsync(connectionId, lakehouseId, tableName, request);
            var statusCode = ResultExtensions.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<ConfirmationResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<ConfirmationResponse>(statusCode, response.Errors));

            throw new NotImplementedException();
        }
    }
}
