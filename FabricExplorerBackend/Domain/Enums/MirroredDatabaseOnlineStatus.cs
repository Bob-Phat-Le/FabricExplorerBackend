namespace FabricExplorerBackend.Domain.Enums
{
    // Trạng thái hiển thị trên giao diện (Active/Offline), suy ra từ MirroringStatus
    public enum MirroredDatabaseOnlineStatus
    {
        Active,
        Offline,
        Unknown
    }
}
