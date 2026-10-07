using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Warehouse;
using FabricExplorerBackend.Features.Fabric.Warehouse;
using FabricExplorerBackend.Features.Result;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Controllers
{
    [Route("/api/workspaces/{workspaceId}/[controller]")]
    [ApiController]
    public class WarehousesController(IWarehouseService warehouseService) : ControllerBase
    {
        // Dropdown: toàn bộ warehouse (kèm workspace) mà các connection đang lưu nhìn thấy
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<WarehouseListItemResponse>>>> GetAllWarehouses(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            CancellationToken cancellationToken = default)
        {
            var response = await warehouseService.GetAllWarehousesAsync(connectionId, workspaceId);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<IEnumerable<WarehouseListItemResponse>>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<IEnumerable<WarehouseListItemResponse>>(statusCode, response.Errors));
        }

        // X-Connection-Id nên là connectionId của dòng được chọn trong dropdown
        [HttpGet("{warehouseId:guid}")]
        public async Task<ActionResult<ApiResponse<WarehouseDetailResponse>>> GetWarehouse(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid warehouseId,
            CancellationToken cancellationToken = default)
        {
            var response = await warehouseService.GetWarehouseByIdAsync(connectionId, workspaceId, warehouseId);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<WarehouseDetailResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<WarehouseDetailResponse>(statusCode, response.Errors));
        }

        [HttpGet("{warehouseId:guid}/connection-string")]
        public async Task<ActionResult<ApiResponse<WarehouseConnectionStringResponse>>> GetConnectionString(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid warehouseId,
            CancellationToken cancellationToken = default)
        {
            var response = await warehouseService.GetConnectionStringAsync(connectionId, workspaceId, warehouseId);
            var statusCode = ResultMapper.ToHttpStatusCode(response.ResultStatus);
            if (response.IsSuccess)
                return Ok(new ApiResponse<WarehouseConnectionStringResponse>(statusCode, response.Data));
            return StatusCode(statusCode, new ApiResponse<WarehouseConnectionStringResponse>(statusCode, response.Errors));
        }
    }
}
