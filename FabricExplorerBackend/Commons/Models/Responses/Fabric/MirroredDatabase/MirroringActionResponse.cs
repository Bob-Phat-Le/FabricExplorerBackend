namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase
{
    public class MirroringActionResponse
    {
        public string Message { get; set; } = null!;
        // Id operation trong database của backend, có khi Fabric trả về LRO (202); dùng GET /api/operations/{id} để theo dõi
        public Guid? OperationId { get; set; }
    }
}
