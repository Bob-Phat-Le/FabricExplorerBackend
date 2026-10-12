using Azure;
using Azure.Identity;
using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public partial class LakehouseService
    {
        public async Task<Result<IEnumerable<LakehouseListItemResponse>>> GetAllLakehousesAsync(
            Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

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
    }
}
