namespace FabricExplorerBackend.Infrastructures.Fabric.Dtos
{
    public class FabricLakehouseDetailResponse
    {
        public string DisplayName { get; set; } = string.Empty;
        public FabricLakehouseProperties? Properties { get; set; }
    }

    public class FabricLakehouseProperties
    {
        public FabricSqlEndpointProperties? SqlEndpointProperties { get; set; }
    }

    public class FabricSqlEndpointProperties
    {
        public string? ConnectionString { get; set; }
        public string? ProvisioningStatus { get; set; }
    }
}
