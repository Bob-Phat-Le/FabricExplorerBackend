using Azure;
using Azure.Identity;
using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Lakehouse;
using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public partial class LakehouseService
    {
        public async Task<Result<IEnumerable<LakehouseListItemResponse>>> GetAllLakehousesAsync(
            Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
                if (connection == null)
                    return Result<IEnumerable<LakehouseListItemResponse>>.Failure(ResultStatus.NotFound, "connection not found");

                var ttl = TimeSpan.FromSeconds(configuration.GetValue("Fabric:ItemListCacheSeconds", 60));
                var key = cachedFetcher.CreateKey("lakehouses", connectionId.ToString(), workspaceId.ToString());

                var items = await cachedFetcher.GetOrFetchAsync<IReadOnlyList<LakehouseListItemResponse>>(
                    key, ttl,
                    async ct =>
                    {
                        // Quy tắc của fetch dùng chung: chỉ dùng dữ liệu thuần + context, không dùng UnitOfWork
                        var context = await fabricContextFactory.CreateFabricContextWithoutTokenAsync(connection, ct)
                            ?? throw new InvalidOperationException("can not create context");

                        var workspaceTask = context.Client.Core.Workspaces.GetWorkspaceAsync(workspaceId, cancellationToken: ct);
                        var lakehousesTask = context.Client.Lakehouse.Items
                            .ListLakehousesAsync(workspaceId)
                            .ToListAsync(ct)
                            .AsTask();

                        await Task.WhenAll(workspaceTask, lakehousesTask);

                        var workspaceName = (await workspaceTask).Value.DisplayName;
                        return (await lakehousesTask)
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
                    },
                    bypassCache,
                    cancellationToken: cancellationToken);

                return Result<IEnumerable<LakehouseListItemResponse>>.Success(items);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
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
            catch (Exception ex)
            {
                return Result<IEnumerable<LakehouseListItemResponse>>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }
    }
}
