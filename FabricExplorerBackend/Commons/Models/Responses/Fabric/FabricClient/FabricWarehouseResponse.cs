namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient
{
    public class FabricWarehouseResponse
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid WorkspaceId { get; set; }
        public FabricWarehouseProperties? Properties { get; set; }
    }

    public class FabricWarehouseProperties
    {
        public string? ConnectionString { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastUpdatedTime { get; set; }
    }
}
