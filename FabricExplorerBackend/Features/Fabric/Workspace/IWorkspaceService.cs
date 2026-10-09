using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Workspace;

namespace FabricExplorerBackend.Features.Fabric.Workspace
{
    public interface IWorkspaceService
    {
        Task<Result<IEnumerable<WorkspaceListItemResponse>>> GetAllWorkspacesAsync(
            Guid connectionId, bool bypassCache = false, CancellationToken cancellationToken = default);
    }
}
