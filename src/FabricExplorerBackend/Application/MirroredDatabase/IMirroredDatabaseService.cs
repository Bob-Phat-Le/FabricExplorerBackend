using FabricExplorerBackend.Application.EventStreaming;
using FabricExplorerBackend.Application.MirroredDatabase.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.MirroredDatabase
{
    public interface IMirroredDatabaseService
    {
        Task<Result<IEnumerable<MirroredDatabaseListItemResponse>>> ListMirroredDatabasesAsync(Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default);
        Task<Result<EventStreamingAdapter<MirroredDatabaseDetailResponse>>> GetMirroredDatabaseByIdAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default);
        Task<Result<EventStreamingAdapter<MirroringDatabaseStatusResponse>>> GetMirroringStatusAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<TableMirroringStatusResponse>>> ListTablesMirroringStatusAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default);
        Task<Result<MirroringActionResponse>> StartMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default);
        Task<Result<MirroringActionResponse>> StopMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default);
        Task StreamDetailAsync(Domain.Entities.Connection connection, Guid workspaceId, Guid mirroredDatabaseId, bool includeDefinition, EventStreamContext stream);
        Task StreamMetricsAsync(Domain.Entities.Connection connection, Guid workspaceId, Guid mirroredDatabaseId, EventStreamContext stream);
        Task<Result<LandingZoneListResponse>> ListObjectsInLandingZoneAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, string? path, LandingZoneRequestQuery query, CancellationToken cancellationToken = default);
        Task<Result<ConfirmationResponse>> CreateFolderAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, string? path, CancellationToken cancellationToken = default);
        Task<Result<ConfirmationResponse>> DeleteFolderAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, string? path, CancellationToken cancellationToken = default);
    }
}
