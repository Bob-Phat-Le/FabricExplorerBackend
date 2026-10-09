using Azure;
using Azure.Identity;
using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Cache;
using FabricExplorerBackend.Features.Fabric.Context;
using FabricExplorerBackend.Features.Fabric.Operation;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Features.Fabric.MirroredDatabase
{
    public class MirroredDatabaseService(
        IMirroredDatabaseMapper mapper,
        IFabricContextFactory fabricContextFactory,
        IFabricOperationMapper fabricOperationMapper,
        IFabricOperationService fabricOperationService,
        ICachedFetcher cachedFetcher,
        IConfiguration configuration,
        IUnitOfWork unitOfWork,
        ILogger<MirroredDatabaseService> logger) : IMirroredDatabaseService
    {
        private const string MirroringDefinitionFileName = "mirroring.json";

        // ---------------------------------------------------------------- Get Mirrored DB

        // Danh sách nhẹ: chỉ id/tên/workspace. Status và nguồn của từng DB là các lời gọi riêng (status + definition)
        // nên chỉ lấy ở endpoint chi tiết, khi người dùng thật sự chọn một DB.
        public async Task<Result<IEnumerable<MirroredDatabaseListItemResponse>>> ListMirroredDatabasesAsync(
            Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var connection = await LoadConnectionAsync(connectionId, cancellationToken);

                var ttl = TimeSpan.FromSeconds(configuration.GetValue("Fabric:ItemListCacheSeconds", 60));
                var key = cachedFetcher.CreateKey("mirrored-dbs", connectionId.ToString(), workspaceId.ToString());

                var items = await cachedFetcher.GetOrFetchAsync<IReadOnlyList<MirroredDatabaseListItemResponse>>(
                    key, ttl,
                    async ct =>
                    {
                        // Quy tắc của fetch dùng chung: chỉ dùng dữ liệu thuần + context, không dùng UnitOfWork
                        var context = await CreateContextAsync(connection, ct);

                        var workspaceTask = context.Client.Core.Workspaces.GetWorkspaceAsync(workspaceId, cancellationToken: ct);
                        var databasesTask = context.Client.MirroredDatabase.Items
                            .ListMirroredDatabasesAsync(workspaceId)
                            .ToListAsync(ct)
                            .AsTask();

                        await Task.WhenAll(workspaceTask, databasesTask);

                        var workspaceName = (await workspaceTask).Value.DisplayName;
                        return (await databasesTask)
                            .Select(database => new MirroredDatabaseListItemResponse
                            {
                                MirroredDatabaseId = (Guid)database.Id!,
                                MirroredDatabaseName = database.DisplayName,
                                WorkspaceId = workspaceId,
                                WorkspaceName = workspaceName,
                                ConnectionId = connectionId
                            })
                            .OrderBy(item => item.MirroredDatabaseName, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                    },
                    bypassCache,
                    cancellationToken: cancellationToken);

                return Result<IEnumerable<MirroredDatabaseListItemResponse>>.Success(items);
            }
            catch (Exception ex)
            {
                return Fail<IEnumerable<MirroredDatabaseListItemResponse>>(ex, cancellationToken);
            }
        }

        public async Task<Result<MirroredDatabaseDetailResponse>> GetMirroredDatabaseByIdAsync(
            Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default)
        {
            try
            {
                var context = await CreateContextAsync(connectionId, cancellationToken);

                var databaseTask = context.Client.MirroredDatabase.Items.GetMirroredDatabaseAsync(workspaceId, mirroredDatabaseId, cancellationToken: cancellationToken);
                var workspaceTask = context.Client.Core.Workspaces.GetWorkspaceAsync(workspaceId, cancellationToken: cancellationToken);
                var statusTask = GetMirroringStatusValueAsync(context, workspaceId, mirroredDatabaseId, cancellationToken);
                var definitionTask = GetDefinitionInfoAsync(context, workspaceId, mirroredDatabaseId, cancellationToken);

                await Task.WhenAll(databaseTask, workspaceTask, statusTask, definitionTask);

                var response = mapper.Map(
                    (await databaseTask).Value,
                    workspaceId,
                    (await workspaceTask).Value.DisplayName,
                    await statusTask,
                    await definitionTask);

                return Result<MirroredDatabaseDetailResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Fail<MirroredDatabaseDetailResponse>(ex, cancellationToken);
            }
        }

        // ---------------------------------------------------------------- Get Status

        public async Task<Result<MirroringDatabaseStatusResponse>> GetMirroringStatusAsync(
            Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default)
        {
            try
            {
                var context = await CreateContextAsync(connectionId, cancellationToken);

                var statusTask = context.Client.MirroredDatabase.Mirroring.GetMirroringStatusAsync(workspaceId, mirroredDatabaseId, cancellationToken: cancellationToken);
                var tablesTask = context.Client.MirroredDatabase.Mirroring
                    .GetTablesMirroringStatusAsync(workspaceId, mirroredDatabaseId)
                    .ToListAsync(cancellationToken)
                    .AsTask();

                var mirroringStatus = (await statusTask).Value;

                // Khi mirroring chưa chạy Fabric có thể từ chối trả trạng thái bảng; vẫn trả về status của database
                var tables = await ListOrEmptyOnRejectedAsync(tablesTask);

                var response = mapper.Map(mirroringStatus);
                response.LastSynchronization = tables.Select(item => item.Metrics?.LastSyncDateTime).Max();
                response.RecordSynchronized = tables.Sum(item => item.Metrics?.ProcessedRows ?? 0);
                response.CurrentLatency = tables.Select(item => item.Metrics?.LastSyncLatencyInSeconds).Max();

                return Result<MirroringDatabaseStatusResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Fail<MirroringDatabaseStatusResponse>(ex, cancellationToken);
            }
        }

        // ---------------------------------------------------------------- Tables Status

        public async Task<Result<IEnumerable<Commons.Models.Responses.Fabric.MirroredDatabase.TableMirroringStatusResponse>>> ListTablesMirroringStatusAsync(
            Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var context = await CreateContextAsync(connectionId, cancellationToken);

                var tablesTask = context.Client.MirroredDatabase.Mirroring
                    .GetTablesMirroringStatusAsync(workspaceId, mirroredDatabaseId)
                    .ToListAsync(cancellationToken)
                    .AsTask();
                var definitionTask = GetDefinitionInfoAsync(context, workspaceId, mirroredDatabaseId, cancellationToken);

                await Task.WhenAll(tablesTask, definitionTask);

                var definition = await definitionTask;
                var response = (await tablesTask)
                    .Select(item => mapper.MapTable(
                        item.SourceSchemaName,
                        item.SourceTableName,
                        item.Status,
                        item.Metrics?.LastSyncDateTime,
                        item.Metrics?.LastSyncLatencyInSeconds,
                        item.Metrics?.ProcessedRows ?? 0,
                        definition))
                    .OrderBy(item => item.Source, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return Result<IEnumerable<Commons.Models.Responses.Fabric.MirroredDatabase.TableMirroringStatusResponse>>.Success(response);
            }
            catch (Exception ex)
            {
                return Fail<IEnumerable<Commons.Models.Responses.Fabric.MirroredDatabase.TableMirroringStatusResponse>>(ex, cancellationToken);
            }
        }

        // ---------------------------------------------------------------- Start / Stop

        public Task<Result<MirroringActionResponse>> StartMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default)
            => ChangeMirroringAsync(connectionId, workspaceId, mirroredDatabaseId, start: true, cancellationToken);

        public Task<Result<MirroringActionResponse>> StopMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default)
            => ChangeMirroringAsync(connectionId, workspaceId, mirroredDatabaseId, start: false, cancellationToken);

        private async Task<Result<MirroringActionResponse>> ChangeMirroringAsync(
            Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, bool start, CancellationToken cancellationToken)
        {
            try
            {
                var context = await CreateContextAsync(connectionId, cancellationToken);
                var action = start ? "start" : "stop";

                // Chặn sớm các thao tác thừa để trả lỗi rõ ràng thay vì lỗi chung từ Fabric (bước đọc: hủy được)
                var current = await GetMirroringStatusValueAsync(context, workspaceId, mirroredDatabaseId, cancellationToken);
                if (start && current is Domain.Enums.MirroringStatus.Running or Domain.Enums.MirroringStatus.Starting)
                    return Result<MirroringActionResponse>.Failure(ResultStatus.Conflict, $"Mirroring is already {current}.");
                if (!start && current is Domain.Enums.MirroringStatus.Stopped or Domain.Enums.MirroringStatus.Stopping)
                    return Result<MirroringActionResponse>.Failure(ResultStatus.Conflict, $"Mirroring is already {current}.");

                // Từ đây trở đi là THAY ĐỔI trạng thái: KHÔNG gắn với token của request. Nếu client ngắt kết nối giữa chừng,
                // ta sẽ không biết Fabric đã nhận lệnh hay chưa, và có thể mất bản ghi operation mà worker cần để theo dõi.
                var response = start
                    ? await context.Client.MirroredDatabase.Mirroring.StartMirroringAsync(workspaceId, mirroredDatabaseId, cancellationToken: CancellationToken.None)
                    : await context.Client.MirroredDatabase.Mirroring.StopMirroringAsync(workspaceId, mirroredDatabaseId, cancellationToken: CancellationToken.None);

                var result = new MirroringActionResponse
                {
                    Message = start
                        ? "Start mirroring requested. Poll the status endpoint until it becomes Running."
                        : "Stop mirroring requested. Poll the status endpoint until it becomes Stopped."
                };

                // Start/Stop hiện trả 200, nhưng nếu Fabric trả 202 (LRO) thì lưu operation để worker poll
                if (response.Status == StatusCodes.Status202Accepted)
                {
                    try
                    {
                        var operationRequest = fabricOperationMapper.Map(response);
                        operationRequest.OperationType = start ? "StartMirroring" : "StopMirroring";
                        operationRequest.ConnectionId = connectionId;
                        operationRequest.WorkspaceId = workspaceId;
                        operationRequest.ResourceId = mirroredDatabaseId;

                        var operation = await fabricOperationService.CreateFabricOperationAsync(operationRequest);
                        if (operation.IsSuccess)
                            result.OperationId = operation.Data?.Id;
                        else
                            logger.LogWarning("Fabric accepted {Action} mirroring but the operation was not saved: {Errors}", action, string.Join("; ", operation.Errors));
                    }
                    catch (Exception ex) when (ex is FormatException or InvalidOperationException or ArgumentException)
                    {
                        logger.LogWarning(ex, "Fabric accepted {Action} mirroring but its operation headers could not be read.", action);
                    }
                }

                return Result<MirroringActionResponse>.Success(result);
            }
            catch (Exception ex)
            {
                return Fail<MirroringActionResponse>(ex, cancellationToken);
            }
        }

        // ---------------------------------------------------------------- Helpers

        private sealed class ServiceFailure(ResultStatus status, string message) : Exception(message)
        {
            public ResultStatus Status { get; } = status;
        }

        private async Task<Domain.Entities.Connection> LoadConnectionAsync(Guid connectionId, CancellationToken cancellationToken)
            => await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken)
                ?? throw new ServiceFailure(ResultStatus.NotFound, "connection not found");

        private async Task<FabricContext> CreateContextAsync(Domain.Entities.Connection connection, CancellationToken cancellationToken)
            => await fabricContextFactory.CreateFabricContextWithoutTokenAsync(connection, cancellationToken)
                ?? throw new ServiceFailure(ResultStatus.BadRequest, "can not create context");

        private async Task<FabricContext> CreateContextAsync(Guid connectionId, CancellationToken cancellationToken)
            => await CreateContextAsync(await LoadConnectionAsync(connectionId, cancellationToken), cancellationToken);

        private static Result<T> Fail<T>(Exception ex, CancellationToken cancellationToken)
        {
            // Client đã hủy request: để ASP.NET dừng lặng lẽ thay vì đổi thành lỗi 500
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();

            return ex switch
            {
                ServiceFailure failure => Result<T>.Failure(failure.Status, failure.Message),
                AuthenticationFailedException => Result<T>.Failure(ResultStatus.BadRequest, ex.Message),
                RequestFailedException { Status: 404 } => Result<T>.Failure(ResultStatus.NotFound, "Mirrored database or workspace not found, or the connection can not access it."),
                RequestFailedException { Status: 401 or 403 } => Result<T>.Failure(ResultStatus.Forbidden, "The connection does not have access to this workspace."),
                RequestFailedException { Status: 400 } => Result<T>.Failure(ResultStatus.BadRequest, ex.Message),
                _ => Result<T>.Failure(ResultStatus.InternalError, ex.Message)
            };
        }

        private async Task<Domain.Enums.MirroringStatus> GetMirroringStatusValueAsync(
            FabricContext context, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await context.Client.MirroredDatabase.Mirroring.GetMirroringStatusAsync(workspaceId, mirroredDatabaseId, cancellationToken: cancellationToken);
                return mapper.MapStatus(response.Value.Status);
            }
            catch (RequestFailedException ex)
            {
                logger.LogWarning(ex, "Cannot read mirroring status of {MirroredDatabaseId}.", mirroredDatabaseId);
                return Domain.Enums.MirroringStatus.Unknown;
            }
        }

        // mirroring.json cho biết nguồn và schema đích. Definition là lời gọi nặng nhất và gần như không đổi,
        // nên cache lâu (mặc định 60 phút) và gộp các lời gọi đồng thời (cùng cơ chế với danh sách).
        private async Task<MirroringDefinitionInfo?> GetDefinitionInfoAsync(
            FabricContext context, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken)
        {
            var ttl = TimeSpan.FromMinutes(configuration.GetValue("Fabric:MirroringDefinitionCacheMinutes", 60));
            var key = cachedFetcher.CreateKey("mirrored-db-definition", workspaceId.ToString(), mirroredDatabaseId.ToString());

            try
            {
                return await cachedFetcher.GetOrFetchAsync<MirroringDefinitionInfo?>(
                    key, ttl,
                    async ct =>
                    {
                        var response = await context.Client.MirroredDatabase.Items
                            .GetMirroredDatabaseDefinitionAsync(workspaceId, mirroredDatabaseId, cancellationToken: ct);
                        var payload = response.Value.Definition?.Parts?
                            .FirstOrDefault(part => part.Path.Equals(MirroringDefinitionFileName, StringComparison.OrdinalIgnoreCase))?
                            .Payload;

                        // null (không đọc được) sẽ không được cache
                        return MirroringDefinitionInfo.Parse(payload);
                    },
                    cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Thiếu thông tin nguồn không nên làm hỏng cả response
                logger.LogWarning(ex, "Cannot read definition of mirrored database {MirroredDatabaseId}.", mirroredDatabaseId);
                return null;
            }
        }

        private static async Task<IReadOnlyList<T>> ListOrEmptyOnRejectedAsync<T>(Task<List<T>> task)
        {
            try
            {
                return await task;
            }
            catch (RequestFailedException ex) when (ex.Status is 400 or 409)
            {
                return [];
            }
        }
    }
}
