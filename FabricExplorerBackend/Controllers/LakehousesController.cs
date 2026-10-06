using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Paginations;
using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.Paginations;
using FabricExplorerBackend.Features.Fabric.Lakehouse;
using FabricExplorerBackend.Features.Result;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LakehousesController(ILakehouseService lakehouseService) : ControllerBase
    {

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
