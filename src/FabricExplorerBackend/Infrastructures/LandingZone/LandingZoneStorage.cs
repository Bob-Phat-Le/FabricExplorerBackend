using Azure;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using FabricExplorerBackend.Infrastructures.Identity;

namespace FabricExplorerBackend.Infrastructures.LandingZone
{
    public sealed class LandingZoneStorage(ITokenService tokenService) : ILandingZoneStorage
    {
        public ILandingZoneFileSystem Open(Domain.Entities.Connection connection, LandingZoneLocation location)
        {
            var credential = tokenService.CreateClientSecretCredential(connection.TenantId, connection.ClientId, connection.ClientSecret);

            var fileSystem = new DataLakeServiceClient(location.Endpoint, credential)
                .GetFileSystemClient(location.FileSystem);

            return new LandingZoneFileSystem(fileSystem, location.RootPath);
        }
    }

    internal sealed class LandingZoneFileSystem(DataLakeFileSystemClient fileSystem, string rootPath) : ILandingZoneFileSystem
    {
        private readonly int rootSegmentCount = rootPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;

        private string Full(string relativePath) => LandingZonePaths.Combine(rootPath, relativePath);

        private string ToRelative(string fullName)
            => string.Join('/', fullName.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(rootSegmentCount));

        private StorageEntry ToEntry(PathItem item)
        {
            var relative = ToRelative(item.Name);
            DateTimeOffset? lastModified = item.LastModified;
            var isDirectory = item.IsDirectory == true;

            return new StorageEntry(
                relative,
                LandingZonePaths.NameOf(relative),
                isDirectory,
                isDirectory ? 0 : Convert.ToInt64(item.ContentLength),
                lastModified,
                string.IsNullOrWhiteSpace(item.Owner) ? null : item.Owner);
        }

        public async Task<StorageListing> ListAsync(
            string relativePath, bool recursive, int maxResults, int maxScanned,
            Func<StorageEntry, bool>? match, CancellationToken cancellationToken)
        {
            var full = Full(relativePath);
            var entries = new List<StorageEntry>();
            var scanned = 0;
            var truncated = false;

            await foreach (var item in fileSystem.GetPathsAsync(
                               path: string.IsNullOrEmpty(full) ? null : full,
                               recursive: recursive,
                               userPrincipalName: false,
                               cancellationToken: cancellationToken))
            {
                if (++scanned > maxScanned)
                {
                    truncated = true;
                    break;
                }

                var entry = ToEntry(item);
                if (match is not null && !match(entry)) continue;

                if (entries.Count >= maxResults)
                {
                    truncated = true;
                    break;
                }

                entries.Add(entry);
            }

            return new StorageListing(entries, truncated);
        }

        public async Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken)
        {
            var full = Full(relativePath);

            // The file system root itself.
            if (string.IsNullOrEmpty(full))
                return (await fileSystem.ExistsAsync(cancellationToken)).Value;

            return (await fileSystem.GetFileClient(full).ExistsAsync(cancellationToken)).Value;
        }

        public async Task<StorageFileInfo> GetFileInfoAsync(string relativePath, CancellationToken cancellationToken)
        {
            var properties = (await fileSystem.GetFileClient(Full(relativePath))
                .GetPropertiesAsync(cancellationToken: cancellationToken)).Value;

            return new StorageFileInfo(
                properties.ContentLength,
                properties.LastModified,
                properties.CreatedOn,
                properties.ContentType);
        }

        public async Task CreateDirectoryAsync(string relativePath, CancellationToken cancellationToken)
        {
            await fileSystem.GetDirectoryClient(Full(relativePath)).CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }

        public async Task UploadAsync(string relativePath, Stream content, bool overwrite, CancellationToken cancellationToken)
        {
            await fileSystem.GetFileClient(Full(relativePath)).UploadAsync(content, overwrite, cancellationToken);
        }

        public async Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken)
        {
            return await fileSystem.GetFileClient(Full(relativePath)).OpenReadAsync(cancellationToken: cancellationToken);
        }

        public async Task<byte[]> ReadHeadAsync(string relativePath, long fileSize, int maxBytes, CancellationToken cancellationToken)
        {
            var length = Math.Min(fileSize, maxBytes);
            if (length <= 0) return [];

            var response = await fileSystem.GetFileClient(Full(relativePath))
                .ReadAsync(new DataLakeFileReadOptions { Range = new HttpRange(0, length) }, cancellationToken: cancellationToken);

            await using var content = response.Value.Content;
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }

        public async Task RenameAsync(string sourcePath, string destinationPath, bool isFolder, CancellationToken cancellationToken)
        {
            if (isFolder)
                await fileSystem.GetDirectoryClient(Full(sourcePath))
                    .RenameAsync(Full(destinationPath), cancellationToken: cancellationToken);
            else
                await fileSystem.GetFileClient(Full(sourcePath))
                    .RenameAsync(Full(destinationPath), cancellationToken: cancellationToken);
        }

        public async Task CopyAsync(string sourcePath, string destinationPath, bool isFolder, CancellationToken cancellationToken)
        {
            if (!isFolder)
            {
                await CopyFileAsync(sourcePath, destinationPath, cancellationToken);
                return;
            }

            await CreateDirectoryAsync(destinationPath, cancellationToken);

            var full = Full(sourcePath);
            await foreach (var item in fileSystem.GetPathsAsync(
                               path: full,
                               recursive: true,
                               userPrincipalName: false,
                               cancellationToken: cancellationToken))
            {
                var relative = ToRelative(item.Name);
                var tail = relative.Length > sourcePath.Length ? relative[sourcePath.Length..].TrimStart('/') : string.Empty;
                if (tail.Length == 0) continue;

                var target = LandingZonePaths.Combine(destinationPath, tail);
                if (item.IsDirectory == true)
                    await CreateDirectoryAsync(target, cancellationToken);
                else
                    await CopyFileAsync(relative, target, cancellationToken);
            }
        }

        private async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            await using var source = await OpenReadAsync(sourcePath, cancellationToken);
            await UploadAsync(destinationPath, source, overwrite: false, cancellationToken);
        }

        public async Task DeleteAsync(string relativePath, bool isFolder, CancellationToken cancellationToken)
        {
            if (isFolder)
                await fileSystem.GetDirectoryClient(Full(relativePath))
                    .DeleteAsync(recursive: true, cancellationToken: cancellationToken);
            else
                await fileSystem.GetFileClient(Full(relativePath))
                    .DeleteAsync(cancellationToken: cancellationToken);
        }
    }
}
