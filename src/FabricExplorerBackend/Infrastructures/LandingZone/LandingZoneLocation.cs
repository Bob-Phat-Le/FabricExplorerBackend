namespace FabricExplorerBackend.Infrastructures.LandingZone
{
    /// <summary>Resolved storage location of a Landing Zone.</summary>
    /// <param name="Endpoint">DFS endpoint, scheme and host only.</param>
    /// <param name="FileSystem">OneLake workspace or ADLS container.</param>
    /// <param name="RootPath">Folder acting as the root of the Landing Zone ("" = file system root).</param>
    public sealed record LandingZoneLocation(Uri Endpoint, string FileSystem, string RootPath)
    {
        /// <summary>abfss://{file system}@{host}/{root}[/{relative}]</summary>
        public string ToUri(string relativePath = "")
        {
            var path = LandingZonePaths.Combine(RootPath, relativePath);
            return $"abfss://{FileSystem}@{Endpoint.Host}/{path}";
        }
    }

    public sealed record StorageEntry(
        string Path,
        string Name,
        bool IsDirectory,
        long SizeBytes,
        DateTimeOffset? LastModified,
        string? Owner);

    public sealed record StorageListing(IReadOnlyList<StorageEntry> Entries, bool Truncated);

    public sealed record StorageFileInfo(
        long SizeBytes,
        DateTimeOffset? LastModified,
        DateTimeOffset? CreatedOn,
        string? ContentType);
}
