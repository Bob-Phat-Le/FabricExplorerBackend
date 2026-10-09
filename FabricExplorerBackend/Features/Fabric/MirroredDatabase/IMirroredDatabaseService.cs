using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase;

namespace FabricExplorerBackend.Features.Fabric.MirroredDatabase
{
    public interface IMirroredDatabaseService
    {
        // Danh sách nhẹ cho dropdown (không gọi status/definition của từng DB)
        Task<Result<IEnumerable<MirroredDatabaseListItemResponse>>> ListMirroredDatabasesAsync(Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default);
        Task<Result<MirroredDatabaseDetailResponse>> GetMirroredDatabaseByIdAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default);
        Task<Result<MirroringDatabaseStatusResponse>> GetMirroringStatusAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<TableMirroringStatusResponse>>> ListTablesMirroringStatusAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default);
        Task<Result<MirroringActionResponse>> StartMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default);
        Task<Result<MirroringActionResponse>> StopMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default);
    }
}
