using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Features.Workspace.Contracts;

namespace FabricExplorerBackend.Features.Workspace
{
    public interface IWorkspaceService
    {
        Task<Result<IEnumerable<WorkspaceListItemResponse>>> GetAllWorkspacesAsync(
            Guid connectionId, bool bypassCache = false, CancellationToken cancellationToken = default);
    }
}
