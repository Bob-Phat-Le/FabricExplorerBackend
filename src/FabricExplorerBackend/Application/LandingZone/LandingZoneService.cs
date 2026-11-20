using Azure;
using FabricExplorerBackend.Application.LandingZone.Contracts.Explorers;
using FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Formats;
using FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Generation;
using FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Xsd;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Helpers;
using FabricExplorerBackend.Infrastructures.Helpers.Factories;
using FabricExplorerBackend.Infrastructures.Helpers.HelperConcretes.LandingZone;
using FabricExplorerBackend.Infrastructures.LandingZone;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Application.LandingZone
{
    public partial class LandingZoneService(
        IHelpersFactory helpersFactory,
        ILogger<LandingZoneService> logger,
        IUnitOfWork unitOfWork,
        ILandingZoneStorage storage,
        IOptions<LandingZoneOptions> landingZoneOptions,
        TimeProvider timeProvider,
        IXsdAnalyzer xsdAnalyzer,
        IMetadataGenerator metadataGenerator,
        IMetadataFormatRegistry metadataFormats) : ILandingZoneService
    {
        private readonly LandingZoneOptions options = landingZoneOptions.Value;
        private readonly ILandingZoneHelper landingZoneHelper = helpersFactory.GetHelper<ILandingZoneHelper>(HelperNames.LandingZoneHelper);

        public async Task<Result<LandingZoneConfigResponse>> GetConfigAsync(Guid connectionId, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection is null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var config = await unitOfWork.LandingZoneRepository.GetConfigAsync(connectionId, cancellationToken: cancellationToken);

            if (config is null)
                return Result<LandingZoneConfigResponse>.Success(new LandingZoneConfigResponse(false, null, null, null, null, null));

            var location = new LandingZoneLocation(new Uri(config.StorageEndpoint), config.FileSystem, config.RootPath);
            return Result<LandingZoneConfigResponse>.Success(new LandingZoneConfigResponse(
                true, config.StorageEndpoint, config.FileSystem, config.RootPath, location.ToUri(), config.UpdatedAt ?? config.CreatedAt));
        }

        public async Task<Result<LandingZoneConfigResponse>> ConfigureAsync(
            Guid connectionId, ConfigureLandingZoneRequest request, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection is null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var parsed = landingZoneHelper.BuildLocation(connection, request);
            if (!parsed.IsSuccess)
                return Result<LandingZoneConfigResponse>.Failure(parsed.Errors.ToArray());

            var location = parsed.Data!;

            // Never save a location the connection cannot read.
            try
            {
                await storage.Open(connection, location).ListAsync(string.Empty, recursive: false, maxResults: 1, maxScanned: 1, match: null, cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return Error.BadRequest(
                    ErrorCodes.LandingZone.PathNotFound,
                    $"The Landing Zone location '{location.ToUri()}' was not found.");
            }

            var now = timeProvider.GetUtcNow();
            var config = await unitOfWork.LandingZoneRepository.GetConfigAsync(connectionId, trackChanges: true, cancellationToken);

            if (config is null)
            {
                config = new LandingZoneConfig
                {
                    Id = Guid.NewGuid(),
                    ConnectionId = connectionId,
                    CreatedAt = now,
                    StorageEndpoint = location.Endpoint.GetLeftPart(UriPartial.Authority),
                    FileSystem = location.FileSystem,
                    RootPath = location.RootPath
                };

                await unitOfWork.LandingZoneRepository.AddConfigAsync(config, cancellationToken);
            }
            else
            {
                config.StorageEndpoint = location.Endpoint.GetLeftPart(UriPartial.Authority);
                config.FileSystem = location.FileSystem;
                config.RootPath = location.RootPath;

                config.UpdatedAt = now;
                unitOfWork.LandingZoneRepository.UpdateConfig(config);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (config is null)
                return Result<LandingZoneConfigResponse>.Success(new LandingZoneConfigResponse(false, null, null, null, null, null));

            var landingZoneLocation = new LandingZoneLocation(new Uri(config.StorageEndpoint), config.FileSystem, config.RootPath);
            return Result<LandingZoneConfigResponse>.Success(new LandingZoneConfigResponse(
                true, config.StorageEndpoint, config.FileSystem, config.RootPath, landingZoneLocation.ToUri(), config.UpdatedAt ?? config.CreatedAt));
        }

        public async Task<Result<ConfirmationResponse>> RemoveConfigAsync(Guid connectionId, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection is null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var config = await unitOfWork.LandingZoneRepository.GetConfigAsync(connectionId, trackChanges: true, cancellationToken);
            if (config is not null)
            {
                unitOfWork.LandingZoneRepository.DeleteConfig(config);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Landing Zone configuration removed." });
        }

        // =====================================================================
        // Browsing
        // =====================================================================

        public async Task<Result<LandingZoneListResponse>> ListItemsAsync(Guid connectionId, string? path, CancellationToken cancellationToken = default)
        {
            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess) return Result<LandingZoneListResponse>.Failure(opened.Errors.ToArray());

            var folder = LandingZonePaths.Normalize(path);

            StorageListing listing;
            try
            {
                listing = await opened.Data!.FileSystem.ListAsync(folder, recursive: false, options.MaxListItems, int.MaxValue, match: null, cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return Error.NotFound(ErrorCodes.LandingZone.PathNotFound, $"Folder '{LandingZonePaths.Display(folder)}' was not found.");
            }

            return Result<LandingZoneListResponse>.Success(
                new LandingZoneListResponse(folder, listing.Entries.Select(entry => new LandingZoneItemResponse(
                    entry.Name,
                    entry.Path,
                    entry.IsDirectory,
                    entry.IsDirectory ? null : LandingZoneContentTypes.ExtensionOf(entry.Name),
                    entry.IsDirectory ? null : entry.SizeBytes,
                    entry.LastModified,
                    entry.Owner)
                )
                .ToList(), listing.Truncated));
        }

        public async Task<Result<LandingZoneFoldersResponse>> ListFoldersAsync(Guid connectionId, string? path, CancellationToken cancellationToken = default)
        {
            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess) return Result<LandingZoneFoldersResponse>.Failure(opened.Errors.ToArray());

            var folder = LandingZonePaths.Normalize(path);

            StorageListing listing;
            try
            {
                listing = await opened.Data!.FileSystem.ListAsync(
                    folder, recursive: false, options.MaxListItems, options.MaxSearchScanned,
                    match: entry => entry.IsDirectory, cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return Error.NotFound(ErrorCodes.LandingZone.PathNotFound, $"Folder '{LandingZonePaths.Display(folder)}' was not found.");
            }

            var folders = listing.Entries
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .Select(e => new LandingZoneFolderResponse(e.Name, e.Path))
                .ToList();

            return Result<LandingZoneFoldersResponse>.Success(new LandingZoneFoldersResponse(folder, folders, listing.Truncated));
        }

        public async Task<Result<LandingZoneListResponse>> SearchAsync(
            Guid connectionId, string? path, string? query, CancellationToken cancellationToken = default)
        {
            var term = query?.Trim();
            if (string.IsNullOrEmpty(term))
                return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, "Search text is required.");

            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess) return Result<LandingZoneListResponse>.Failure(opened.Errors.ToArray());

            var folder = LandingZonePaths.Normalize(path);

            StorageListing listing;
            try
            {
                listing = await opened.Data!.FileSystem.ListAsync(
                    folder, recursive: true, options.MaxSearchResults, options.MaxSearchScanned,
                    match: entry => entry.Name.Contains(term, StringComparison.OrdinalIgnoreCase), cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return Error.NotFound(ErrorCodes.LandingZone.PathNotFound, $"Folder '{LandingZonePaths.Display(folder)}' was not found.");
            }

            return Result<LandingZoneListResponse>.Success(
                new LandingZoneListResponse(folder, listing.Entries.Select(entry => new LandingZoneItemResponse(
                    entry.Name,
                    entry.Path,
                    entry.IsDirectory,
                    entry.IsDirectory ? null : LandingZoneContentTypes.ExtensionOf(entry.Name),
                    entry.IsDirectory ? null : entry.SizeBytes,
                    entry.LastModified,
                    entry.Owner)
                )
                .ToList(), listing.Truncated));
        }

        public async Task<Result<LandingZoneFileDetailsResponse>> GetFileDetailsAsync(Guid connectionId, string? path, CancellationToken cancellationToken = default)
        {
            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess)
                return Result<LandingZoneFileDetailsResponse>.Failure(opened.Errors.ToArray());

            var file = LandingZonePaths.Normalize(path);
            if (file.Length == 0)
                throw new Commons.Errors.ApplicationException(Error.BadRequest(ErrorCodes.LandingZone.InvalidPath, "A file or folder path is required."));

            var session = opened.Data!;

            StorageFileInfo info;
            try
            {
                info = await session.FileSystem.GetFileInfoAsync(file, cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return Error.NotFound(ErrorCodes.LandingZone.PathNotFound, $"File '{LandingZonePaths.Display(file)}' was not found.");
            }

            var name = LandingZonePaths.NameOf(file);

            return Result<LandingZoneFileDetailsResponse>.Success(
                new LandingZoneFileDetailsResponse(
                    name,
                    file,
                    LandingZoneContentTypes.ExtensionOf(name),
                    info.SizeBytes,
                    LandingZoneContentTypes.FromFileName(name),
                    info.CreatedOn,
                    info.LastModified,
                    session.Location.ToUri(file)));
        }

        public async Task<Result<LandingZonePreviewResponse>> GetPreviewAsync(Guid connectionId, string? path, CancellationToken cancellationToken = default)
        {
            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess)
                return Result<LandingZonePreviewResponse>.Failure(opened.Errors.ToArray());

            var file = LandingZonePaths.Normalize(path);
            if (file.Length == 0)
                throw new Commons.Errors.ApplicationException(Error.BadRequest(ErrorCodes.LandingZone.InvalidPath, "A file or folder path is required."));

            var name = LandingZonePaths.NameOf(file);

            if (!LandingZonePreviewBuilder.IsPreviewable(name))
                return Result<LandingZonePreviewResponse>.Success(LandingZonePreviewBuilder.Unsupported());

            var fileSystem = opened.Data!.FileSystem;

            try
            {
                var info = await fileSystem.GetFileInfoAsync(file, cancellationToken);
                var head = await fileSystem.ReadHeadAsync(file, info.SizeBytes, options.PreviewBytes, cancellationToken);

                return Result<LandingZonePreviewResponse>.Success(LandingZonePreviewBuilder.Build(name, head, info.SizeBytes, options.PreviewCsvRows));
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return Error.NotFound(ErrorCodes.LandingZone.PathNotFound, $"File '{LandingZonePaths.Display(file)}' was not found.");
            }
        }

        // =====================================================================
        // Download / upload
        // =====================================================================

        public async Task<Result<LandingZoneDownload>> DownloadFileAsync(Guid connectionId, string? path, CancellationToken cancellationToken = default)
        {
            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess)
                return Result<LandingZoneDownload>.Failure(opened.Errors.ToArray());

            var file = LandingZonePaths.Normalize(path);
            if (file.Length == 0)
                throw new Commons.Errors.ApplicationException(Error.BadRequest(ErrorCodes.LandingZone.InvalidPath, "A file or folder path is required."));

            var name = LandingZonePaths.NameOf(file);

            Stream content;
            try
            {
                content = await opened.Data!.FileSystem.OpenReadAsync(file, cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return Error.NotFound(ErrorCodes.LandingZone.PathNotFound, $"File '{LandingZonePaths.Display(file)}' was not found.");
            }

            await FlushLogAsync(connectionId,
            [
                NewLog(connectionId, "Download", name, LandingZonePaths.ParentOf(file), true, "File downloaded")
            ]);

            return Result<LandingZoneDownload>.Success(new LandingZoneDownload(content, name, LandingZoneContentTypes.FromFileName(name)));
        }

        public async Task<Result<LandingZoneDownload>> DownloadArchiveAsync(Guid connectionId, DownloadArchiveRequest request, CancellationToken cancellationToken = default)
        {
            var paths = (request.Paths ?? [])
                .Select(LandingZonePaths.Normalize)
                .Where(p => p.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (paths.Count == 0)
                return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, "Select at least one file to download.");

            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess)
                return Result<LandingZoneDownload>.Failure(opened.Errors.ToArray());

            var archive = await landingZoneHelper.CreateArchiveAsync(opened.Data!, paths, cancellationToken);

            await FlushLogAsync(connectionId, [
                NewLog(connectionId, "Download", $"{paths.Count} paths (zip)", string.Empty, true, "Archive created successfully")
            ]);

            return Result<LandingZoneDownload>.Success(
                new LandingZoneDownload(archive, "landing-zone-files.zip", "application/zip"));
        }

        public async Task<Result<LandingZoneBatchResponse>> UploadAsync(
            Guid connectionId, string? path, bool overwrite, IReadOnlyList<LandingZoneUploadFile> files,
            CancellationToken cancellationToken = default)
        {
            if (files.Count == 0)
                return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, "No files were uploaded.");

            if (files.Count > options.MaxBatchItems)
                return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, $"At most {options.MaxBatchItems} files can be uploaded at once.");

            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess) return Result<LandingZoneBatchResponse>.Failure(opened.Errors.ToArray());

            var session = opened.Data!;
            var destination = LandingZonePaths.Normalize(path);

            var missing = await landingZoneHelper.EnsureFolderExistsAsync(session, destination, cancellationToken);
            if (missing is not null) return missing;

            var log = new List<LandingZoneOperation>();
            var results = new List<LandingZoneItemResult>();

            try
            {
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fileName = System.IO.Path.GetFileName(file.FileName);
                    var displayName = string.IsNullOrWhiteSpace(fileName) ? "(unnamed)" : fileName;

                    results.Add(await RunItemAsync(
                        connectionId, log, "Upload", displayName, destination,
                        resultPath: LandingZonePaths.Combine(destination, fileName),
                        async () =>
                        {
                            var name = LandingZonePaths.ValidateName(fileName);
                            var target = LandingZonePaths.Combine(destination, name);

                            await using var content = file.OpenReadStream();
                            await session.FileSystem.UploadAsync(target, content, overwrite, cancellationToken);

                            var bytes = file.Length;
                            string[] units = ["B", "KB", "MB", "GB", "TB"];
                            var index = bytes <= 0 ? 0 : Math.Min((int)Math.Floor(Math.Log(bytes, 1024)), units.Length - 1);
                            var formattedSize = bytes <= 0 ? "0 B" : $"{Math.Round(bytes / Math.Pow(1024, index), 1)} {units[index]}";

                            return $"{formattedSize} uploaded";
                        }));
                }
            }
            finally
            {
                await FlushLogAsync(connectionId, log);
            }

            return Result<LandingZoneBatchResponse>.Success(
                new LandingZoneBatchResponse(
                    results,
                    results.Count(r => r.Success),
                    results.Count(r => !r.Success)));
        }

        // =====================================================================
        // Organize
        // =====================================================================

        public async Task<Result<LandingZoneItemResponse>> CreateFolderAsync(
            Guid connectionId, CreateFolderRequest request, CancellationToken cancellationToken = default)
        {
            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess) return Result<LandingZoneItemResponse>.Failure(opened.Errors.ToArray());

            var session = opened.Data!;
            var parent = LandingZonePaths.Normalize(request.ParentPath);
            var name = LandingZonePaths.ValidateName(request.Name);
            var path = LandingZonePaths.Combine(parent, name);

            var missing = await landingZoneHelper.EnsureFolderExistsAsync(session, parent, cancellationToken);
            if (missing is not null) return missing;

            if (await session.FileSystem.ExistsAsync(path, cancellationToken))
            {
                await FlushLogAsync(connectionId, [
                    NewLog(connectionId, "Create Folder", name, parent, false, "A folder or file with this name already exists")
                ]);
                return Error.Conflict(ErrorCodes.LandingZone.AlreadyExists, $"'{name}' already exists in this folder.");
            }

            try
            {
                await session.FileSystem.CreateDirectoryAsync(path, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var message = ex switch
                {
                    RequestFailedException azure =>
                        $"HTTP {azure.Status}{(string.IsNullOrEmpty(azure.ErrorCode) ? string.Empty : $" {azure.ErrorCode}")}: {azure.Message.Split('\n', 2)[0].Trim()}",
                    Commons.Errors.ApplicationException app => app.Error.Message,
                    _ => ex.Message
                };

                await FlushLogAsync(connectionId, [
                    NewLog(connectionId, "Create Folder", name, parent, false, message)
                ]);
                throw;
            }

            await FlushLogAsync(connectionId, [
                NewLog(connectionId, "Create Folder", name, parent, true, "Directory created")
            ]);

            return Result<LandingZoneItemResponse>.Success(
                new LandingZoneItemResponse(name, path, true, null, null, timeProvider.GetUtcNow(), null));
        }

        public async Task<Result<LandingZoneItemResponse>> RenameAsync(
            Guid connectionId, RenameItemRequest request, CancellationToken cancellationToken = default)
        {
            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess)
                return Result<LandingZoneItemResponse>.Failure(opened.Errors.ToArray());

            var session = opened.Data!;

            var path = LandingZonePaths.Normalize(request.Path);
            if (path.Length == 0)
                throw new Commons.Errors.ApplicationException(Error.BadRequest(ErrorCodes.LandingZone.InvalidPath, "A file or folder path is required."));

            var newName = LandingZonePaths.ValidateName(request.NewName);
            var oldName = LandingZonePaths.NameOf(path);
            var parent = LandingZonePaths.ParentOf(path);
            var target = LandingZonePaths.Combine(parent, newName);
            var label = $"{oldName} → {newName}";

            if (string.Equals(oldName, newName, StringComparison.Ordinal))
                return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, "The new name is the same as the current name.");

            try
            {
                if (await session.FileSystem.ExistsAsync(target, cancellationToken))
                {
                    await FlushLogAsync(connectionId,
                    [
                        NewLog(connectionId, "Rename", label, parent, false, "An item with this name already exists")
                    ]);

                    return Error.Conflict(ErrorCodes.LandingZone.AlreadyExists, $"'{newName}' already exists in this folder.");
                }

                await session.FileSystem.RenameAsync(path, target, request.IsFolder, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var message = ex switch
                {
                    RequestFailedException azure =>
                        $"HTTP {azure.Status}{(string.IsNullOrEmpty(azure.ErrorCode) ? string.Empty : $" {azure.ErrorCode}")}: {azure.Message.Split('\n', 2)[0].Trim()}",
                    Commons.Errors.ApplicationException app => app.Error.Message,
                    _ => ex.Message
                };

                await FlushLogAsync(connectionId,
                [
                    NewLog(connectionId, "Rename", label, parent, false, message)
                ]);

                throw;
            }

            await FlushLogAsync(connectionId,
            [
                NewLog(connectionId, "Rename", label, parent, true, "Renamed")
            ]);

            return Result<LandingZoneItemResponse>.Success(
                new LandingZoneItemResponse(
                    newName,
                    target,
                    request.IsFolder,
                    request.IsFolder ? null : LandingZoneContentTypes.ExtensionOf(newName),
                    null,
                    null,
                    null));
        }

        public Task<Result<LandingZoneBatchResponse>> MoveAsync(
            Guid connectionId, TransferItemsRequest request, CancellationToken cancellationToken = default)
            => TransferAsync(connectionId, request, move: true, cancellationToken);

        public Task<Result<LandingZoneBatchResponse>> CopyAsync(
            Guid connectionId, TransferItemsRequest request, CancellationToken cancellationToken = default)
            => TransferAsync(connectionId, request, move: false, cancellationToken);

        private async Task<Result<LandingZoneBatchResponse>> TransferAsync(
            Guid connectionId, TransferItemsRequest request, bool move, CancellationToken cancellationToken)
        {
            if (request.Items is null || request.Items.Count == 0)
                return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, "Select at least one item.");

            if (request.Items.Count > options.MaxBatchItems)
                return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, $"At most {options.MaxBatchItems} items can be processed at once.");

            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess) return Result<LandingZoneBatchResponse>.Failure(opened.Errors.ToArray());

            var session = opened.Data!;
            var destination = LandingZonePaths.Normalize(request.DestinationPath);

            var missing = await landingZoneHelper.EnsureFolderExistsAsync(session, destination, cancellationToken);
            if (missing is not null) return missing;

            var operation = move ? "Move" : "Copy";
            var log = new List<LandingZoneOperation>();
            var results = new List<LandingZoneItemResult>();

            try
            {
                foreach (var item in request.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rawPath = item.Path ?? string.Empty;
                    var displayName = LandingZonePaths.NameOf(rawPath.Trim('/'));

                    results.Add(await RunItemAsync(
                        connectionId, log, operation, displayName, destination, resultPath: rawPath,
                        async () =>
                        {
                            var source = LandingZonePaths.Normalize(item.Path);
                            if (source.Length == 0)
                                throw new Commons.Errors.ApplicationException(Error.Conflict(ErrorCodes.LandingZone.BadRequest, "The Landing Zone root cannot be moved or copied."));

                            var target = LandingZonePaths.Combine(destination, LandingZonePaths.NameOf(source));

                            if (item.IsFolder && LandingZonePaths.IsSameOrDescendant(destination, source))
                                throw new Commons.Errors.ApplicationException(Error.Conflict(ErrorCodes.LandingZone.BadRequest, "A folder cannot be moved or copied into itself."));

                            if (move && LandingZonePaths.ParentOf(source) == destination)
                                throw new Commons.Errors.ApplicationException(Error.Conflict(ErrorCodes.LandingZone.BadRequest, "The item is already in this folder."));

                            if (await session.FileSystem.ExistsAsync(target, cancellationToken))
                                throw new Commons.Errors.ApplicationException(Error.Conflict(ErrorCodes.LandingZone.BadRequest, "An item with this name already exists in the destination folder."));

                            if (move)
                                await session.FileSystem.RenameAsync(source, target, item.IsFolder, cancellationToken);
                            else
                                await session.FileSystem.CopyAsync(source, target, item.IsFolder, cancellationToken);

                            return $"{(move ? "Moved" : "Copied")} from {LandingZonePaths.Display(LandingZonePaths.ParentOf(source))}";
                        }));
                }
            }
            finally
            {
                await FlushLogAsync(connectionId, log);
            }

            return Result<LandingZoneBatchResponse>.Success(
                new LandingZoneBatchResponse(
                    results,
                    results.Count(r => r.Success),
                    results.Count(r => !r.Success)));
        }

        public async Task<Result<LandingZoneBatchResponse>> DeleteAsync(
            Guid connectionId, DeleteItemsRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Items is null || request.Items.Count == 0)
                return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, "Select at least one item.");

            if (request.Items.Count > options.MaxBatchItems)
                return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, $"At most {options.MaxBatchItems} items can be processed at once.");

            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess)
                return Result<LandingZoneBatchResponse>.Failure(opened.Errors.ToArray());

            var session = opened.Data!;
            var log = new List<LandingZoneOperation>();
            var results = new List<LandingZoneItemResult>();

            try
            {
                foreach (var item in request.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rawPath = item.Path ?? string.Empty;
                    var displayName = LandingZonePaths.NameOf(rawPath.Trim('/'));
                    var parent = LandingZonePaths.ParentOf(rawPath.Trim('/'));

                    results.Add(await RunItemAsync(
                        connectionId,
                        log,
                        "Delete",
                        displayName,
                        parent,
                        resultPath: rawPath,
                        async () =>
                        {
                            var path = LandingZonePaths.Normalize(item.Path);
                            if (path.Length == 0)
                                throw new Commons.Errors.ApplicationException(Error.Conflict(ErrorCodes.LandingZone.BadRequest, "The Landing Zone root cannot be deleted."));

                            await session.FileSystem.DeleteAsync(path, item.IsFolder, cancellationToken);
                            return item.IsFolder ? "Folder deleted" : "File deleted";
                        }));
                }
            }
            finally
            {
                await FlushLogAsync(connectionId, log);
            }

            return Result<LandingZoneBatchResponse>.Success(
                new LandingZoneBatchResponse(
                    results,
                    results.Count(r => r.Success),
                    results.Count(r => !r.Success)));
        }

        // =====================================================================
        // History
        // =====================================================================

        public async Task<Result<IReadOnlyList<LandingZoneOperationResponse>>> GetOperationsAsync(
            Guid connectionId, int limit, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection is null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var take = Math.Clamp(limit, 1, Math.Max(1, options.HistoryRetention));
            var operations = await unitOfWork.LandingZoneRepository.GetOperationsAsync(connectionId, take, cancellationToken);

            IReadOnlyList<LandingZoneOperationResponse> response = operations
                .Select(o => new LandingZoneOperationResponse(o.Id, o.Operation, o.Item, o.Path, o.Status, o.Details, o.CreatedAt))
                .ToList();

            return Result<IReadOnlyList<LandingZoneOperationResponse>>.Success(response);
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private async Task<LandingZoneItemResult> RunItemAsync(
            Guid connectionId, List<LandingZoneOperation> log, string operation, string itemName, string logFolder,
            string resultPath, Func<Task<string>> action)
        {
            try
            {
                var details = await action();
                log.Add(NewLog(connectionId, operation, itemName, logFolder, true, details));
                return new LandingZoneItemResult(resultPath, itemName, true, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var reason = ex switch
                {
                    RequestFailedException azure =>
                        $"HTTP {azure.Status}{(string.IsNullOrEmpty(azure.ErrorCode) ? string.Empty : $" {azure.ErrorCode}")}: {azure.Message.Split('\n', 2)[0].Trim()}",
                    Commons.Errors.ApplicationException app => app.Error.Message,
                    _ => ex.Message
                };

                logger.LogWarning("Landing Zone {Operation} of '{Item}' failed: {Reason}", operation, itemName, reason);
                log.Add(NewLog(connectionId, operation, itemName, logFolder, false, reason));
                return new LandingZoneItemResult(resultPath, itemName, false, reason);
            }
        }

        private LandingZoneOperation NewLog(
            Guid connectionId, string operation, string item, string folder, bool success, string? details)
            => LandingZoneOperation.Create(
                connectionId, operation, item, LandingZonePaths.Display(folder), success, details, timeProvider.GetUtcNow());

        private async Task FlushLogAsync(Guid connectionId, List<LandingZoneOperation> log)
        {
            if (log.Count == 0) return;

            try
            {
                await unitOfWork.LandingZoneRepository.AddOperationsAsync(log, CancellationToken.None);
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
                await unitOfWork.LandingZoneRepository.TrimOperationsAsync(connectionId, options.HistoryRetention, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not record Landing Zone operations of connection {ConnectionId}", connectionId);
            }
        }
    }
}
