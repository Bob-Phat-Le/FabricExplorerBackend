namespace FabricExplorerBackend.Commons.Models.Responses.Warehouse
{
    /// <summary>
    /// Một dòng của dropdown. Khi người dùng chọn dòng này, frontend gọi các endpoint chi tiết với
    /// X-Connection-Id = ConnectionId (connection đã nhìn thấy warehouse này) và workspaceId/warehouseId tương ứng.
    /// </summary>
    public class WarehouseListItemResponse
    {
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = null!;
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = null!;
        public Guid ConnectionId { get; set; }
    }
}
