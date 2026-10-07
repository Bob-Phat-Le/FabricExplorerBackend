namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient
{
    // Map response của GET /capacities (state: Active | Inactive)
    public class FabricCapacityResponse
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? Region { get; set; }
        public string? State { get; set; }
    }
}
