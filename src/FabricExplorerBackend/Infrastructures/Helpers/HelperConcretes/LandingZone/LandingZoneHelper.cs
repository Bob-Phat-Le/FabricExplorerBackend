using Azure;
using FabricExplorerBackend.Application.LandingZone.Contracts.Explorers;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.LandingZone;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using System.IO.Compression;

namespace FabricExplorerBackend.Infrastructures.Helpers.HelperConcretes.LandingZone
{
    public class LandingZoneHelper(
        IOptions<LandingZoneOptions> config,
        IUnitOfWork unitOfWork,
        ILandingZoneStorage storage) : ILandingZoneHelper
    {
        private readonly LandingZoneOptions options = config.Value;

        public Result<LandingZoneLocation> BuildLocation(Connection connection, ConfigureLandingZoneRequest request)
        {
            string host;
            string fileSystem;
            string root;

            if (request.MirroredDatabaseId is { } mirroredDatabaseId)
            {
                if (mirroredDatabaseId == Guid.Empty)
                    return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, "Mirrored database id is invalid.");
                if (!Uri.TryCreate(options.OneLakeEndpoint, UriKind.Absolute, out var oneLake))
                    return Error.BadRequest(ErrorCodes.LandingZone.UnsupportedEndpoint, "The OneLake endpoint is not configured correctly.");

                host = oneLake.IdnHost;
                fileSystem = connection.WorkspaceId.ToString("D");
                root = $"{mirroredDatabaseId:D}/Files/LandingZone";
            }
            else
            {
                var text = request.StorageUri?.Trim();
                if (string.IsNullOrEmpty(text))
                    return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, "Choose a mirrored database or enter a storage URI.");
                if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
                    return Error.BadRequest(ErrorCodes.LandingZone.UnsupportedEndpoint, "The storage URI is not valid.");
                if (uri.Port is not (-1 or 443))
                    return Error.BadRequest(ErrorCodes.LandingZone.UnsupportedEndpoint, "Custom ports are not supported.");

                switch (uri.Scheme.ToLowerInvariant())
                {
                    case "abfss":
                        // abfss://{file system}@{host}/{path}
                        fileSystem = Uri.UnescapeDataString(uri.UserInfo);
                        root = LandingZonePaths.Normalize(Uri.UnescapeDataString(uri.AbsolutePath));
                        break;

                    case "https":
                        // https://{host}/{file system}/{path}
                        var segments = Uri.UnescapeDataString(uri.AbsolutePath).Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (segments.Length == 0)
                            return Error.BadRequest(
                                ErrorCodes.LandingZone.BadRequest,
                                "The URI must include the workspace or container, e.g. https://onelake.dfs.fabric.microsoft.com/<workspace>/<item>/Files/LandingZone");

                        fileSystem = segments[0];
                        root = LandingZonePaths.Normalize(string.Join('/', segments.Skip(1)));
                        break;

                    default:
                        return Error.BadRequest(ErrorCodes.LandingZone.UnsupportedEndpoint, "Only abfss:// and https:// URIs are supported.");
                }

                host = uri.IdnHost;
            }

            host = host.ToLowerInvariant();
            if (!options.AllowedEndpointSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
                return Error.BadRequest(
                    ErrorCodes.LandingZone.UnsupportedEndpoint,
                    $"Storage host '{host}' is not allowed. Allowed hosts end with: {string.Join(", ", options.AllowedEndpointSuffixes)}.");

            if
            (
                !(
                    fileSystem.Length is > 0 and <= LandingZoneConfig.FileSystemMaxLength
                    && char.IsLetterOrDigit(fileSystem[0])
                    && fileSystem.All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-' or ' ')
                )
            )
                return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, "The workspace or container name is not valid.");

            return Result<LandingZoneLocation>.Success(new LandingZoneLocation(new Uri($"https://{host}"), fileSystem, root));
        }

        public async Task<Result<Session>> OpenAsync(Guid connectionId, CancellationToken cancellationToken)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection is null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var config = await unitOfWork.LandingZoneRepository.GetConfigAsync(connectionId, cancellationToken: cancellationToken);
            if (config is null)
                return Error.Conflict(ErrorCodes.LandingZone.NotConfigured, "Landing Zone is not configured for this connection.");

            var location = new LandingZoneLocation(new Uri(config.StorageEndpoint), config.FileSystem, config.RootPath);
            return Result<Session>.Success(new Session(connection, location, storage.Open(connection, location)));
        }

        public async Task<Error?> EnsureFolderExistsAsync(Session session, string folder, CancellationToken cancellationToken)
        {
            if (folder.Length == 0) return null;

            try
            {
                return await session.FileSystem.ExistsAsync(folder, cancellationToken) ? null : Error.NotFound(ErrorCodes.LandingZone.PathNotFound, $"Folder '{LandingZonePaths.Display(folder)}' was not found.");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return Error.NotFound(ErrorCodes.LandingZone.PathNotFound, $"Folder '{LandingZonePaths.Display(folder)}' was not found.");
            }
        }

        public async Task<Stream> CreateArchiveAsync(Session session, IReadOnlyList<string> paths, CancellationToken cancellationToken)
        {
            var archive = new FileStream(
                Path.Combine(Path.GetTempPath(), $"landing-zone-{Guid.NewGuid():N}.zip"),
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                81920,
                FileOptions.DeleteOnClose | FileOptions.Asynchronous);

            try
            {
                using (var zip = new ZipArchive(archive, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var fileCount = 0;

                    foreach (var path in paths)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var normalizedPath = LandingZonePaths.Normalize(path);
                        var parentPath = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/') ?? string.Empty;

                        var listing = await session.FileSystem.ListAsync(
                            parentPath,
                            recursive: false,
                            maxResults: options.MaxListItems,
                            maxScanned: options.MaxSearchScanned,
                            match: null,
                            cancellationToken);

                        if (listing.Truncated)
                            throw new InvalidOperationException("The directory listing exceeded the configured limit.");

                        var entry = listing.Entries.FirstOrDefault(x => string.Equals(x.Path, normalizedPath, StringComparison.Ordinal));

                        if (entry is null)
                            throw new FileNotFoundException($"Path '{normalizedPath}' was not found.");

                        await AddToArchiveAsync(session.FileSystem, zip, entry, entry.Name, () => ++fileCount, cancellationToken);
                    }
                }

                archive.Position = 0;
                return archive;
            }
            catch
            {
                await archive.DisposeAsync();
                throw;
            }
        }

        private async Task AddToArchiveAsync(
            ILandingZoneFileSystem fileSystem,
            ZipArchive zip,
            StorageEntry item,
            string archivePath,
            Func<int> incrementFileCount,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!item.IsDirectory)
            {
                if (incrementFileCount() > options.MaxArchiveFiles)
                    throw new InvalidOperationException($"At most {options.MaxArchiveFiles} files can be downloaded at once.");

                var zipEntry = zip.CreateEntry(archivePath, CompressionLevel.Fastest);

                await using var source = await fileSystem.OpenReadAsync(item.Path, cancellationToken);
                await using var target = zipEntry.Open();

                await source.CopyToAsync(target, cancellationToken);
                return;
            }

            var listing = await fileSystem.ListAsync(
                item.Path,
                recursive: false,
                maxResults: options.MaxListItems,
                maxScanned: options.MaxSearchScanned,
                match: null,
                cancellationToken);

            if (listing.Truncated)
                throw new InvalidOperationException("The directory listing exceeded the configured limit.");

            foreach (var child in listing.Entries)
                await AddToArchiveAsync(fileSystem, zip, child, $"{archivePath}/{child.Name}", incrementFileCount, cancellationToken);
        }
    }
}
