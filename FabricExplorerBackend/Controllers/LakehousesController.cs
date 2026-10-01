using FabricExplorerBackend.Commons;
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
            if (response.IsSuccess)
                return Ok(response);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("{lakehouseId}/tables/{tableName}/load")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> LoadTable(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId, 
            Guid lakehouseId,
            string tableName,
            [FromBody] LoadTableRequest request)
        {
            var response = await lakehouseService.LoadTableAsync(connectionId, lakehouseId, tableName, request);
            if (response.IsSuccess)
                return Ok(response);
            return StatusCode(response.StatusCode, response);

            throw new NotImplementedException();
        }
    }
}
