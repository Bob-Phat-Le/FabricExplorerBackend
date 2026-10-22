using FabricExplorerBackend.Application.EventStreaming;
using FabricExplorerBackend.Application.Warhouse.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Application.Warhouse
{
    public class WarehouseService(
        IUnitOfWork unitOfWork,
        IFabricContextFactory fabricContextFactory,
        IEventStreamService eventStreamService,
        IOptions<CacheOptions> cacheOptions,
        ICachedFetcher cachedFetcher,
        ILogger<WarehouseService> logger) : IWarehouseService
    {
        private CacheOptions cacheOptions = cacheOptions.Value;

        public async Task<Result<EventStreamingAdapter<WarehouseDetailResponse>>> GetWarehouseByIdAsync(Guid connectionId, Guid workspaceId, Guid warehouseId, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection is null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found.");

            var context = await fabricContextFactory.CreateFabricContextWithoutTokenAsync(connection, cancellationToken);

            var warehouseTask = context.Client.Warehouse.Items.GetWarehouseAsync(workspaceId, warehouseId, cancellationToken: cancellationToken);
            var workspaceTask = context.Client.Core.Workspaces.GetWorkspaceAsync(workspaceId, cancellationToken: cancellationToken);

            await Task.WhenAll(warehouseTask, workspaceTask);

            var warehouse = (await warehouseTask).Value;
            var workspace = (await workspaceTask).Value;

            var sqlHost = warehouse.Properties?.ConnectionString;
            var needsProbe = !string.IsNullOrWhiteSpace(sqlHost);

            var response = new WarehouseDetailResponse
            {
                Id = warehouseId,
                Name = warehouse.DisplayName,
                Description = warehouse.Description,
                OnlineStatus = (needsProbe ? WarehouseOnlineStatus.Checking : WarehouseOnlineStatus.Unknown).ToString(),
                OnlineStatusMessage = needsProbe ? null : "Warehouse chưa có connection string (có thể đang được khởi tạo).",
                CreatedDate = warehouse?.Properties?.CreatedDate,
                LastUpdatedTime = warehouse?.Properties?.LastUpdatedTime,
                Workspace = new WarehouseWorkspaceContextResponse
                {
                    Id = workspaceId,
                    Name = workspace.DisplayName,
                    CapacityId = Guid.TryParse(workspace.CapacityId?.ToString(), out var capacityId)
                            ? capacityId
                            : null
                }
            };

            if (!needsProbe)
                return Result<EventStreamingAdapter<WarehouseDetailResponse>>.Success(
                    EventStreamingAdapter<WarehouseDetailResponse>.Ready(response));

            var warehouseName = warehouse.DisplayName;
            var adapter = eventStreamService.Stream(
                response,
                pendingFields: [nameof(WarehouseDetailResponse.OnlineStatus), nameof(WarehouseDetailResponse.OnlineStatusMessage)],
                work: async ctx =>
                {
                    var (status, message) = await ProbeOnlineStatusAsync(
                        ctx.Services.GetRequiredService<ISqlQueryService>(), connection, sqlHost!, warehouseName, ctx.CancellationToken);

                    ctx.Publish(WarehouseStreamEvents.OnlineStatus,
                        new WarehouseOnlineStatusStreamEvent(status.ToString(), message));
                });

            return Result<EventStreamingAdapter<WarehouseDetailResponse>>.Success(adapter);
        }

        public async Task<Result<IEnumerable<WarehouseListItemResponse>>> GetAllWarehousesAsync(
            Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var ttl = cacheOptions.ItemList;
            var key = cachedFetcher.CreateKey("warehouses", connectionId.ToString(), workspaceId.ToString());

            var items = await cachedFetcher.GetOrFetchAsync<IReadOnlyList<WarehouseListItemResponse>>(
                key, ttl,
                async ct =>
                {
                    var context = await fabricContextFactory.CreateFabricContextWithoutTokenAsync(connection, ct)
                        ?? throw new InvalidOperationException("can not create context");

                    var workspaceTask = context.Client.Core.Workspaces.GetWorkspaceAsync(workspaceId, cancellationToken: ct);
                    var warehousesTask = context.Client.Warehouse.Items
                        .ListWarehousesAsync(workspaceId)
                        .ToListAsync(ct)
                        .AsTask();

                    await Task.WhenAll(workspaceTask, warehousesTask);

                    var workspaceName = (await workspaceTask).Value.DisplayName;
                    return (await warehousesTask)
                        .Select(wh => new WarehouseListItemResponse
                        {
                            WarehouseName = wh.DisplayName,
                            WorkspaceName = workspaceName,
                            WarehouseId = (Guid)wh.Id!,
                            WorkspaceId = workspaceId,
                            ConnectionId = connectionId
                        })
                        .ToList();
                },
                bypassCache,
                cancellationToken: cancellationToken);

            return Result<IEnumerable<WarehouseListItemResponse>>.Success(items);
        }

        public async Task<Result<WarehouseConnectionStringResponse>> GetConnectionStringAsync(Guid connectionId, Guid workspaceId, Guid warehouseId, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var context = await fabricContextFactory.CreateFabricContextWithoutTokenAsync(connection, cancellationToken);

            var response = await context.Client.Warehouse.Items.GetConnectionStringAsync(workspaceId, warehouseId, cancellationToken: cancellationToken);
            return Result<WarehouseConnectionStringResponse>.Success(new WarehouseConnectionStringResponse
            {
                WarehouseId = warehouseId,
                ConnectionString = response.Value.ConnectionString
            });
        }

        private async Task<(WarehouseOnlineStatus Status, string? Message)> ProbeOnlineStatusAsync(
            ISqlQueryService sqlQueryService,
            Domain.Entities.Connection connection,
            string sqlHost,
            string warehouseName,
            CancellationToken cancellationToken)
        {
            var timeout = cacheOptions.WarehouseProbeTimeout;
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            try
            {
                await sqlQueryService.ExecuteScalarAsync<int?>(
                    connection,
                    new SqlEndpoint(sqlHost, warehouseName),
                    "SELECT 1",
                    cancellationToken: timeoutSource.Token);

                return (WarehouseOnlineStatus.Online, null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return (WarehouseOnlineStatus.Offline, $"Không kết nối được trong {timeout.TotalSeconds:0} giây.");
            }
            catch (SqlException ex)
            {
                logger.LogWarning(ex, "SQL probe failed for warehouse {WarehouseName}.", warehouseName);
                return (WarehouseOnlineStatus.Offline, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot check online status of warehouse {WarehouseName}.", warehouseName);
                return (WarehouseOnlineStatus.Unknown, ExceptionClassifier.Classify(ex).Message);
            }
        }
    }
}
