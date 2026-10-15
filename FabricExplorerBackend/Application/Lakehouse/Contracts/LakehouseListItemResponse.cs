namespace FabricExplorerBackend.Application.Lakehouse.Contracts
{
    public class LakehouseListItemResponse
    {
        public Guid LakehouseId { get; set; }
        public string LakehouseName { get; set; } = null!;
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = null!;
        public Guid ConnectionId { get; set; }
    }
}
