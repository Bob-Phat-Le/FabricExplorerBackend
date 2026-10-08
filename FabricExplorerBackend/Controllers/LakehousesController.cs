using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Paginations;
using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
using FabricExplorerBackend.Commons.Models.Responses.Lakehouse;
using FabricExplorerBackend.Commons.Models.Responses.Paginations;
using FabricExplorerBackend.Features.Fabric.Lakehouse;
using FabricExplorerBackend.Features.Result;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Controllers
{
    [Route("api/workspaces/{workspaceId}/[controller]")]
    [ApiController]
    public class LakehousesController(ILakehouseService lakehouseService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<LakehouseListItemResponse>>>> GetAllLakehouses(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId)
        {
            var response = await lakehouseService.GetAllLakehousesAsync(connectionId, workspaceId);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<IEnumerable<LakehouseListItemResponse>>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<IEnumerable<LakehouseListItemResponse>>(statusCode, response.Errors));
        }

        [HttpGet("{lakehouseId}/tables")]
        public async Task<ActionResult<ApiResponse<PagedResponseWithCursor<IEnumerable<TableResponse>>>>> GetAllTables(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid lakehouseId,
            [FromQuery] PaginationWithCursorRequest? paginationRequest)
        {
            var response = await lakehouseService.GetAllTablesAsync(connectionId, lakehouseId, paginationRequest);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
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
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<ConfirmationResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<ConfirmationResponse>(statusCode, response.Errors));

            throw new NotImplementedException();
        }
    }
}
