namespace FabricExplorerBackend.Infrastructures.LandingZone
{
    /// <summary>Opens a file system view over a configured Landing Zone.</summary>
    public interface ILandingZoneStorage
    {
        ILandingZoneFileSystem Open(Domain.Entities.Connection connection, LandingZoneLocation location);
    }

    /// <summary>
    /// File operations of a Landing Zone. All paths are relative to the Landing Zone root and already
    /// normalized by <see cref="LandingZonePaths"/>. Transport failures surface as
    /// <see cref="Azure.RequestFailedException"/>.
    /// </summary>
    public interface ILandingZoneFileSystem
    {
        /// <summary>Lists a folder (optionally recursively). <paramref name="match"/> filters entries before they count.</summary>
        Task<StorageListing> ListAsync(
            string relativePath, bool recursive, int maxResults, int maxScanned,
            Func<StorageEntry, bool>? match, CancellationToken cancellationToken);

        /// <summary>True when a file or folder exists at the path.</summary>
        Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken);

        Task<StorageFileInfo> GetFileInfoAsync(string relativePath, CancellationToken cancellationToken);

        Task CreateDirectoryAsync(string relativePath, CancellationToken cancellationToken);

        Task UploadAsync(string relativePath, Stream content, bool overwrite, CancellationToken cancellationToken);

        Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken);

        /// <summary>Reads at most <paramref name="maxBytes"/> bytes from the beginning of a file.</summary>
        Task<byte[]> ReadHeadAsync(string relativePath, long fileSize, int maxBytes, CancellationToken cancellationToken);

        Task RenameAsync(string sourcePath, string destinationPath, bool isFolder, CancellationToken cancellationToken);

        /// <summary>Copies a file, or a folder with everything in it. Never overwrites.</summary>
        Task CopyAsync(string sourcePath, string destinationPath, bool isFolder, CancellationToken cancellationToken);

        Task DeleteAsync(string relativePath, bool isFolder, CancellationToken cancellationToken);
    }
}
