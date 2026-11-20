using FabricExplorerBackend.Application.LandingZone.Contracts.Explorers;
using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.LandingZone
{
    public interface ILandingZoneService
    {
        Task<Result<LandingZoneConfigResponse>> GetConfigAsync(Guid connectionId, CancellationToken cancellationToken = default);
        Task<Result<LandingZoneConfigResponse>> ConfigureAsync(Guid connectionId, ConfigureLandingZoneRequest request, CancellationToken cancellationToken = default);
        Task<Result<ConfirmationResponse>> RemoveConfigAsync(Guid connectionId, CancellationToken cancellationToken = default);

        Task<Result<LandingZoneListResponse>> ListItemsAsync(Guid connectionId, string? path, CancellationToken cancellationToken = default);
        Task<Result<LandingZoneFoldersResponse>> ListFoldersAsync(Guid connectionId, string? path, CancellationToken cancellationToken = default);
        Task<Result<LandingZoneListResponse>> SearchAsync(Guid connectionId, string? path, string? query, CancellationToken cancellationToken = default);
        Task<Result<LandingZoneFileDetailsResponse>> GetFileDetailsAsync(Guid connectionId, string? path, CancellationToken cancellationToken = default);
        Task<Result<LandingZonePreviewResponse>> GetPreviewAsync(Guid connectionId, string? path, CancellationToken cancellationToken = default);

        Task<Result<LandingZoneDownload>> DownloadFileAsync(Guid connectionId, string? path, CancellationToken cancellationToken = default);
        Task<Result<LandingZoneDownload>> DownloadArchiveAsync(Guid connectionId, DownloadArchiveRequest request, CancellationToken cancellationToken = default);
        Task<Result<LandingZoneBatchResponse>> UploadAsync(Guid connectionId, string? path, bool overwrite, IReadOnlyList<LandingZoneUploadFile> files, CancellationToken cancellationToken = default);

        Task<Result<LandingZoneItemResponse>> CreateFolderAsync(Guid connectionId, CreateFolderRequest request, CancellationToken cancellationToken = default);
        Task<Result<LandingZoneItemResponse>> RenameAsync(Guid connectionId, RenameItemRequest request, CancellationToken cancellationToken = default);
        Task<Result<LandingZoneBatchResponse>> MoveAsync(Guid connectionId, TransferItemsRequest request, CancellationToken cancellationToken = default);
        Task<Result<LandingZoneBatchResponse>> CopyAsync(Guid connectionId, TransferItemsRequest request, CancellationToken cancellationToken = default);
        Task<Result<LandingZoneBatchResponse>> DeleteAsync(Guid connectionId, DeleteItemsRequest request, CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<LandingZoneOperationResponse>>> GetOperationsAsync(Guid connectionId, int limit, CancellationToken cancellationToken = default);

        Result<MetadataCapabilities> GetMetadataCapabilities();
        Task<Result<AnalyzeResponse>> AnalyzeXsdAsync(AnalyzeRequest request, CancellationToken cancellationToken = default);
        Task<Result<PreviewResponse>> PreviewAsync(PreviewRequest request, CancellationToken cancellationToken = default);
        Task<Result<PublishResponse>> PublishAsync(Guid connectionId, PublishRequest request, CancellationToken cancellationToken = default);
    }
}
