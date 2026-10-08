using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase;

namespace FabricExplorerBackend.Features.Fabric.MirroredDatabase
{
    public interface IMirroredDatabaseService
    {
        Task<Result<IEnumerable<MirroredDatabaseDetailResponse>>> ListMirroredDatabasesAsync(Guid connectionId, Guid workspaceId);
        Task<Result<MirroredDatabaseDetailResponse>> GetMirroredDatabaseByIdAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId);
        Task<Result<MirroringDatabaseStatusResponse>> GetMirroringStatusAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId);
        Task<Result<IEnumerable<TableMirroringStatusResponse>>> ListTablesMirroringStatusAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId);
        Task<Result<MirroringActionResponse>> StartMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId);
        Task<Result<MirroringActionResponse>> StopMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId);
    }
}
