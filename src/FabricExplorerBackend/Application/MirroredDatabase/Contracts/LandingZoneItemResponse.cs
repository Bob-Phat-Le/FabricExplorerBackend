using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Application.MirroredDatabase.Contracts
{
    public class LandingZoneItemResponse
    {
        public string Name { get; set; } = null!;
        public bool IsDirectory { get; set; }
        public DateTimeOffset CreationTime { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public string Etag { get; set; } = null!;
        public long ContentLength { get; set; }
        public Guid Owner { get; set; }
        public Guid Group { get; set; }
        public string Permissions { get; set; } = null!;
        public DateTimeOffset? ExpireTime { get; set; }
    }
}
