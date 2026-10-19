using FabricExplorerBackend.Application.Workspace.Contracts;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Workspace
{
    public interface IWorkspaceService
    {
        Task<Result<IEnumerable<WorkspaceListItemResponse>>> GetAllWorkspacesAsync(
            Guid connectionId, bool bypassCache = false, CancellationToken cancellationToken = default);
    }
}
