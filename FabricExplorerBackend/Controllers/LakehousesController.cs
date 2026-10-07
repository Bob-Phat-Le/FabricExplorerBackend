using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Paginations;
using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation;
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
        public async Task<ActionResult<ApiResponse<FabricOperationResponse>>> LoadTable(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid lakehouseId,
            string tableName,
            [FromBody] LoadTableRequest request)
        {
            var response = await lakehouseService.LoadTableAsync(connectionId, lakehouseId, tableName, request);

            if (response.IsSuccess)
            {
                // 202 Accepted + Location trỏ tới endpoint kiểm tra trạng thái; body có sẵn id để frontend poll
                var location = Url.Action(
                    nameof(OperationsController.GetOperation),
                    "Operations",
                    new { operationId = response.Data!.Id });

                return Accepted(location, new ApiResponse<FabricOperationResponse>(StatusCodes.Status202Accepted, response.Data));
            }

            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            return StatusCode(statusCode, new ApiResponse<FabricOperationResponse>(statusCode, response.Errors));
        }
    }
}
