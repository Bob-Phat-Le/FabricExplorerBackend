using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.Warhouse;
using FabricExplorerBackend.Application.Warhouse.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    [Route("/api/workspaces/{workspaceId}/[controller]")]
    [ApiController]
    public class WarehousesController(IWarehouseService warehouseService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<WarehouseListItemResponse>>>> GetAllWarehouses(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            CancellationToken cancellationToken = default)
            => (await warehouseService.GetAllWarehousesAsync(connectionId, workspaceId, Request.ShouldBypassCache(), cancellationToken)).ToActionResult(this);

        [HttpGet("{warehouseId:guid}")]
        public async Task<ActionResult<ApiResponse<WarehouseDetailResponse>>> GetWarehouse(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid warehouseId,
            CancellationToken cancellationToken = default)
            => (await warehouseService.GetWarehouseByIdAsync(connectionId, workspaceId, warehouseId, cancellationToken)).ToActionResult(this);

        [HttpGet("{warehouseId:guid}/connection-string")]
        public async Task<ActionResult<ApiResponse<WarehouseConnectionStringResponse>>> GetConnectionString(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            Guid workspaceId,
            Guid warehouseId,
            CancellationToken cancellationToken = default)
            => (await warehouseService.GetConnectionStringAsync(connectionId, workspaceId, warehouseId, cancellationToken)).ToActionResult(this);
    }
}
