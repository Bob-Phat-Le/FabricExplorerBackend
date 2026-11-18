using FabricExplorerBackend.Domain.Entities.Abstractions;

namespace FabricExplorerBackend.Domain.Entities
{
    /// <summary>
    /// Where the Landing Zone of a connection lives: any ADLS Gen2 compatible DFS endpoint
    /// (OneLake or an Azure storage account). One row per connection.
    /// </summary>
    public class LandingZoneConfig : BaseEntity
    {
        public const int EndpointMaxLength = 256;
        public const int FileSystemMaxLength = 256;
        public const int RootPathMaxLength = 1024;

        public Guid ConnectionId { get; set; }

        /// <summary>DFS endpoint without path, e.g. https://onelake.dfs.fabric.microsoft.com</summary>
        public string StorageEndpoint { get; set; } = null!;

        /// <summary>OneLake workspace (id or name) or ADLS container.</summary>
        public string FileSystem { get; set; } = null!;

        /// <summary>Folder inside the file system that acts as the root of the Landing Zone ("" = file system root).</summary>
        public string RootPath { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
