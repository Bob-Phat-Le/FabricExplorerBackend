using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.Warehouse
{
    public sealed class WarehouseResponse
    {
        public string Name { get; set; } = null!;
        public WarehouseStatus Status { get; set; }
        public string WorkspaceName { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
