using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Mappers.Result;
using FabricExplorerBackend.Models.Responses;
using FabricExplorerBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LakehousesController(ILakehouseService lakehouseService) : ControllerBase
    {

        [HttpGet("{lakehouseId}/tables")]
        public async Task<ActionResult<ApiResponse<PagedResponse<IEnumerable<Table>>>>> GetAllTables(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid lakehouseId, 
            [FromQuery] PaginationRequest? paginationRequest)
        {
            var response = await lakehouseService.GetAllTablesAsync(connectionId, lakehouseId, paginationRequest);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<PagedResponse<IEnumerable<Table>>>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<PagedResponse<IEnumerable<Table>>>(statusCode, response.Errors));
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
