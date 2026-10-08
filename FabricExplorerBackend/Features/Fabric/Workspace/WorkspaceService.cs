using Azure;
using Azure.Identity;
using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Workspace;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Features.Fabric.Workspace
{
    public class WorkspaceService(
        IUnitOfWork unitOfWork,
        IFabricContextFactory fabricContextFactory) : IWorkspaceService
    {
        public async Task<Result<IEnumerable<WorkspaceListItemResponse>>> GetAllWorkspacesAsync(Guid connectionId)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                    return Result<IEnumerable<WorkspaceListItemResponse>>.Failure(ResultStatus.NotFound, "connection not found");

                var context = await fabricContextFactory.CreateFabricContextAsync(connection);
                if (context == null)
                    return Result<IEnumerable<WorkspaceListItemResponse>>.Failure(ResultStatus.BadRequest, "can not create context");

                // Fabric chỉ trả các workspace mà service principal của connection là thành viên
                var workspaces = await context.Client.Core.Workspaces
                    .ListWorkspacesAsync()
                    .ToListAsync()
                    .AsTask();

                var response = workspaces
                    .Select(w => new WorkspaceListItemResponse
                    {
                        WorkspaceId = (Guid)w.Id!,
                        WorkspaceName = w.DisplayName,
                        CapacityId = w.CapacityId
                    })
                    .OrderBy(w => w.WorkspaceName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return Result<IEnumerable<WorkspaceListItemResponse>>.Success(response);
            }
            catch (AuthenticationFailedException ex)
            {
                return Result<IEnumerable<WorkspaceListItemResponse>>.Failure(ResultStatus.BadRequest, ex.Message);
            }
            catch (RequestFailedException ex) when (ex.Status is 401 or 403)
            {
                return Result<IEnumerable<WorkspaceListItemResponse>>.Failure(
                    ResultStatus.Forbidden, "The connection is not allowed to list workspaces.");
            }
        }
    }
}
