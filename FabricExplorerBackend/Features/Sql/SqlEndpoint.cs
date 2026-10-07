namespace FabricExplorerBackend.Features.Sql
{
    /// <summary>
    /// Đích kết nối SQL của một item trong Fabric (SQL analytics endpoint của lakehouse hoặc warehouse).
    /// </summary>
    /// <param name="Server">Host do Fabric trả về (properties.connectionString), ví dụ xxx.datawarehouse.fabric.microsoft.com.</param>
    /// <param name="Database">Tên database: là tên lakehouse hoặc tên warehouse.</param>
    public sealed record SqlEndpoint(string Server, string Database);
}
