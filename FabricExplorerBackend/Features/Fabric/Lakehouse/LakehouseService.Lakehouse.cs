using Azure;
using Azure.Identity;
using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Lakehouse;
using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public partial class LakehouseService
    {
        public async Task<Result<IEnumerable<LakehouseListItemResponse>>> GetAllLakehousesAsync(Guid connectionId, Guid workspaceId)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                    return Result<IEnumerable<LakehouseListItemResponse>>.Failure(ResultStatus.NotFound, "connection not found");

                var context = await fabricContextFactory.CreateFabricContextAsync(connection);
                if (context == null)
                    return Result<IEnumerable<LakehouseListItemResponse>>.Failure(ResultStatus.BadRequest, "can not create context");

                var workspaceTask = context.Client.Core.Workspaces.GetWorkspaceAsync(workspaceId);
                var lakehousesTask = context.Client.Lakehouse.Items
                    .ListLakehousesAsync(workspaceId)
                    .ToListAsync()
                    .AsTask();

                await Task.WhenAll(workspaceTask, lakehousesTask);

                var workspaceName = (await workspaceTask).Value.DisplayName;
                var response = (await lakehousesTask)
                    .Select(lh => new LakehouseListItemResponse
                    {
                        LakehouseId = (Guid)lh.Id!,
                        LakehouseName = lh.DisplayName,
                        WorkspaceId = workspaceId,
                        WorkspaceName = workspaceName,
                        ConnectionId = connectionId
                    })
                    .OrderBy(item => item.LakehouseName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return Result<IEnumerable<LakehouseListItemResponse>>.Success(response);
            }
            catch (AuthenticationFailedException ex)
            {
                return Result<IEnumerable<LakehouseListItemResponse>>.Failure(ResultStatus.BadRequest, ex.Message);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return Result<IEnumerable<LakehouseListItemResponse>>.Failure(
                    ResultStatus.NotFound, "Workspace not found, or the connection can not access it.");
            }
            catch (RequestFailedException ex) when (ex.Status is 401 or 403)
            {
                return Result<IEnumerable<LakehouseListItemResponse>>.Failure(
                    ResultStatus.Forbidden, "The connection does not have access to this workspace.");
            }
        }
    }
}
