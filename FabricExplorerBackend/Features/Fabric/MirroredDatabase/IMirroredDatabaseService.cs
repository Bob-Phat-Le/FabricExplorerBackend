using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase;

namespace FabricExplorerBackend.Features.Fabric.MirroredDatabase
{
    public interface IMirroredDatabaseService
    {
        Task<Result<MirroredDatabaseDetailResponse>> GetMirroredDatabaseByIdAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId);
        Task<Result<MirroringDatabaseStatusResponse>> GetMirroringStatus(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId);
        Task<Result<IEnumerable<TableMirroringStatusResponse>>> ListTablesMirroringStatusAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId);
    }
}
