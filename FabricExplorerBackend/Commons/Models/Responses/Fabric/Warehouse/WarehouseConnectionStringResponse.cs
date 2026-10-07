namespace FabricExplorerBackend.Commons.Models.Responses.Warehouse
{
    public class WarehouseConnectionStringResponse
    {
        public Guid WarehouseId { get; set; }
        public string ConnectionString { get; set; } = null!;
        public string Database { get; set; } = null!;
    }
}
