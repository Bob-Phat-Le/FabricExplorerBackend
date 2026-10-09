using Azure;
using Azure.Identity;
using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Warehouse;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Cache;
using FabricExplorerBackend.Features.Fabric.Context;
using FabricExplorerBackend.Features.Sql;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace FabricExplorerBackend.Features.Fabric.Warehouse
{
    public class WarehouseService(
        IUnitOfWork unitOfWork,
        IFabricContextFactory fabricContextFactory,
        ISqlQueryService sqlQueryService,
        IConfiguration configuration,
        ICachedFetcher cachedFetcher,
        ILogger<WarehouseService> logger) : IWarehouseService
    {
        public async Task<Result<WarehouseDetailResponse>> GetWarehouseByIdAsync(Guid connectionId, Guid workspaceId, Guid warehouseId, CancellationToken cancellationToken = default)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
                if (connection == null)
                    return Result<WarehouseDetailResponse>.Failure(ResultStatus.NotFound, "connection not found");

                var context = await fabricContextFactory.CreateFabricContextWithoutTokenAsync(connection, cancellationToken);
                if (context == null)
                    return Result<WarehouseDetailResponse>.Failure(ResultStatus.BadRequest, "can not create context");

                var fabricWarehouseTask = context.Client.Warehouse.Items.GetWarehouseAsync(workspaceId, warehouseId, cancellationToken: cancellationToken);
                var fabricWorkspaceTask = context.Client.Core.Workspaces.GetWorkspaceAsync(workspaceId, cancellationToken: cancellationToken);

                await Task.WhenAll(fabricWarehouseTask, fabricWorkspaceTask);

                var warehouse = (await fabricWarehouseTask).Value;
                var workspace = (await fabricWorkspaceTask).Value;

                var (onlineStatus, onlineStatusMessage) = await CheckOnlineStatusAsync(
                    connection, warehouse.Properties?.ConnectionString, warehouse.DisplayName, cancellationToken);

                return Result<WarehouseDetailResponse>.Success(new WarehouseDetailResponse
                {
                    Id = warehouseId,
                    Name = warehouse.DisplayName,
                    Description = warehouse.Description,
                    OnlineStatus = onlineStatus.ToString(),
                    OnlineStatusMessage = onlineStatusMessage,
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
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (AuthenticationFailedException ex)
            {
                return Result<WarehouseDetailResponse>.Failure(ResultStatus.BadRequest, ex.Message);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return Result<WarehouseDetailResponse>.Failure(
                    ResultStatus.NotFound, "Warehouse or workspace not found, or the connection can not access it.");
            }
            catch (RequestFailedException ex) when (ex.Status is 401 or 403)
            {
                return Result<WarehouseDetailResponse>.Failure(
                    ResultStatus.Forbidden, "The connection does not have access to this workspace.");
            }
            catch (Exception ex)
            {
                return Result<WarehouseDetailResponse>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }

        public async Task<Result<IEnumerable<WarehouseListItemResponse>>> GetAllWarehousesAsync(
            Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
                if (connection == null)
                    return Result<IEnumerable<WarehouseListItemResponse>>.Failure(ResultStatus.NotFound, "connection not found");

                var ttl = TimeSpan.FromSeconds(configuration.GetValue("Fabric:ItemListCacheSeconds", 60));
                var key = cachedFetcher.CreateKey("warehouses", connectionId.ToString(), workspaceId.ToString());

                var items = await cachedFetcher.GetOrFetchAsync<IReadOnlyList<WarehouseListItemResponse>>(
                    key, ttl,
                    async ct =>
                    {
                        // Quy tắc của fetch dùng chung: chỉ dùng dữ liệu thuần + context, không dùng UnitOfWork
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (AuthenticationFailedException ex)
            {
                return Result<IEnumerable<WarehouseListItemResponse>>.Failure(ResultStatus.BadRequest, ex.Message);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return Result<IEnumerable<WarehouseListItemResponse>>.Failure(
                    ResultStatus.NotFound, "Workspace not found, or the connection can not access it.");
            }
            catch (RequestFailedException ex) when (ex.Status is 401 or 403)
            {
                return Result<IEnumerable<WarehouseListItemResponse>>.Failure(
                    ResultStatus.Forbidden, "The connection does not have access to this workspace.");
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<WarehouseListItemResponse>>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }

        public async Task<Result<WarehouseConnectionStringResponse>> GetConnectionStringAsync(Guid connectionId, Guid workspaceId, Guid warehouseId, CancellationToken cancellationToken = default)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
                if (connection == null)
                    return Result<WarehouseConnectionStringResponse>.Failure(ResultStatus.NotFound, "connection not found");

                var context = await fabricContextFactory.CreateFabricContextWithoutTokenAsync(connection, cancellationToken);
                if (context == null)
                    return Result<WarehouseConnectionStringResponse>.Failure(ResultStatus.BadRequest, "can not create context");

                var response = await context.Client.Warehouse.Items.GetConnectionStringAsync(workspaceId, warehouseId, cancellationToken: cancellationToken);
                return Result<WarehouseConnectionStringResponse>.Success(new WarehouseConnectionStringResponse
                {
                    WarehouseId = warehouseId,
                    ConnectionString = response.Value.ConnectionString
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<WarehouseConnectionStringResponse>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }

        private async Task<(WarehouseOnlineStatus Status, string? Message)> CheckOnlineStatusAsync(
            Domain.Entities.Connection connection, string? sqlHost, string warehouseName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sqlHost))
                return (WarehouseOnlineStatus.Unknown, "Warehouse chưa có connection string (có thể đang được khởi tạo).");

            var timeout = TimeSpan.FromSeconds(configuration.GetValue("Fabric:WarehouseProbeTimeoutSeconds", 10));
            // Hủy khi request bị hủy HOẶC khi quá thời gian dò
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
                // Ví dụ không lấy được token SQL: chưa đủ cơ sở để kết luận warehouse offline
                logger.LogWarning(ex, "Cannot check online status of warehouse {WarehouseName}.", warehouseName);
                return (WarehouseOnlineStatus.Unknown, ex.Message);
            }
        }
    }
}
