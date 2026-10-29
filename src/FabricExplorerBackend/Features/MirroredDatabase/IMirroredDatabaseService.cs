using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Features.EventStreaming;
using FabricExplorerBackend.Features.MirroredDatabase.Contracts;

namespace FabricExplorerBackend.Features.MirroredDatabase
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
    }
}
