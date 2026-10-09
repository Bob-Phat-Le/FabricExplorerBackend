namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase
{
    /// <summary>
    /// Một dòng của dropdown Mirrored Database: chỉ thông tin định danh, rẻ để lấy.
    /// Nguồn, trạng thái... nằm ở endpoint chi tiết (MirroredDatabaseDetailResponse) và chỉ lấy khi người dùng chọn một DB.
    /// </summary>
    public class MirroredDatabaseListItemResponse
    {
        public Guid MirroredDatabaseId { get; set; }
        public string MirroredDatabaseName { get; set; } = null!;
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = null!;
        public Guid ConnectionId { get; set; }
    }
}
