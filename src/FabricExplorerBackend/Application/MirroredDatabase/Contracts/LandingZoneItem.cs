namespace FabricExplorerBackend.Application.MirroredDatabase.Contracts
{
    public class LandingZoneItem
    {
        public string Name { get; set; } = null!;
        public string? IsDirectory { get; set; }
        public string CreationTime { get; set; } = null!;
        public string LastModified { get; set; } = null!;
        public string Etag { get; set; } = null!;
        public string ContentLength { get; set; } = null!;
        public Guid Owner { get; set; }
        public Guid Group { get; set; }
        public string Permissions { get; set; } = null!;
        public string ExpiryTime { get; set; } = null!;
    }

    public class LandingZoneItemList
    {
        public List<LandingZoneItem> Paths { get; set; } = [];
        public string? ContinuationToken { get; set; }
    }
}
