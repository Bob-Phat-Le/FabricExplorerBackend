namespace FabricExplorerBackend.Commons.Models.Responses.Warehouse
{
    public class WarehouseDetailResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string OnlineStatus { get; set; } = null!;
        public string? OnlineStatusMessage { get; set; }
        public WarehouseWorkspaceContextResponse Workspace { get; set; } = null!;
        public DateTimeOffset? CreatedDate { get; set; }
        public DateTimeOffset? LastUpdatedTime { get; set; }
    }

    public class WarehouseWorkspaceContextResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public Guid? CapacityId { get; set; }
    }
}
