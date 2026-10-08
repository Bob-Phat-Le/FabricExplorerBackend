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
using System.Text.Json;

namespace FabricExplorerBackend.Features.Fabric.MirroredDatabase
{
    public class MirroredDatabaseService(
        IMirroredDatabaseMapper mapper,
        IFabricContextFactory fabricContextFactory,
        IFabricOperationMapper fabricOperationMapper,
        IFabricOperationService fabricOperationService,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        ILogger<MirroredDatabaseService> logger) : IMirroredDatabaseService
    {
        private const string MirroringDefinitionFileName = "mirroring.json";
        private const int MaxParallelFabricCalls = 4;
        private static readonly TimeSpan DefinitionCacheDuration = TimeSpan.FromMinutes(15);

        // ---------------------------------------------------------------- Get Mirrored DB

        public async Task<Result<IEnumerable<MirroredDatabaseDetailResponse>>> ListMirroredDatabasesAsync(Guid connectionId, Guid workspaceId)
        {
            try
            {
                var context = await CreateContextAsync(connectionId);

                var workspaceTask = context.Client.Core.Workspaces.GetWorkspaceAsync(workspaceId);
                var databasesTask = context.Client.MirroredDatabase.Items
                    .ListMirroredDatabasesAsync(workspaceId)
                    .ToListAsync()
                    .AsTask();

                await Task.WhenAll(workspaceTask, databasesTask);

                var workspaceName = (await workspaceTask).Value.DisplayName;
                var databases = await databasesTask;

                // Mỗi database cần thêm 2 call (status + definition) nên giới hạn số call song song
                using var semaphore = new SemaphoreSlim(MaxParallelFabricCalls);
                var items = await Task.WhenAll(databases.Select(async database =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var id = (Guid)database.Id!;
                        var statusTask = GetMirroringStatusValueAsync(context, workspaceId, id);
                        var definitionTask = GetDefinitionInfoAsync(context, workspaceId, id);
                        await Task.WhenAll(statusTask, definitionTask);

                        return mapper.Map(database, workspaceId, workspaceName, await statusTask, await definitionTask);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));

                return Result<IEnumerable<MirroredDatabaseDetailResponse>>.Success(
                    items.OrderBy(item => item.MirroredDatabaseName, StringComparer.OrdinalIgnoreCase).ToList());
            }
            catch (Exception ex)
            {
                return Fail<IEnumerable<MirroredDatabaseDetailResponse>>(ex);
            }
        }

        public async Task<Result<MirroredDatabaseDetailResponse>> GetMirroredDatabaseByIdAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId)
        {
            try
            {
                var context = await CreateContextAsync(connectionId);

                var databaseTask = context.Client.MirroredDatabase.Items.GetMirroredDatabaseAsync(workspaceId, mirroredDatabaseId);
                var workspaceTask = context.Client.Core.Workspaces.GetWorkspaceAsync(workspaceId);
                var statusTask = GetMirroringStatusValueAsync(context, workspaceId, mirroredDatabaseId);
                var definitionTask = GetDefinitionInfoAsync(context, workspaceId, mirroredDatabaseId);

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
                return Fail<MirroredDatabaseDetailResponse>(ex);
            }
        }

        // ---------------------------------------------------------------- Get Status

        public async Task<Result<MirroringDatabaseStatusResponse>> GetMirroringStatusAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId)
        {
            try
            {
                var context = await CreateContextAsync(connectionId);

                var statusTask = context.Client.MirroredDatabase.Mirroring.GetMirroringStatusAsync(workspaceId, mirroredDatabaseId);
                var tablesTask = context.Client.MirroredDatabase.Mirroring
                    .GetTablesMirroringStatusAsync(workspaceId, mirroredDatabaseId)
                    .ToListAsync()
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
                return Fail<MirroringDatabaseStatusResponse>(ex);
            }
        }

        // ---------------------------------------------------------------- Tables Status

        public async Task<Result<IEnumerable<Commons.Models.Responses.Fabric.MirroredDatabase.TableMirroringStatusResponse>>> ListTablesMirroringStatusAsync(
            Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId)
        {
            try
            {
                var context = await CreateContextAsync(connectionId);

                var tablesTask = context.Client.MirroredDatabase.Mirroring
                    .GetTablesMirroringStatusAsync(workspaceId, mirroredDatabaseId)
                    .ToListAsync()
                    .AsTask();
                var definitionTask = GetDefinitionInfoAsync(context, workspaceId, mirroredDatabaseId);

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
                return Fail<IEnumerable<Commons.Models.Responses.Fabric.MirroredDatabase.TableMirroringStatusResponse>>(ex);
            }
        }

        // ---------------------------------------------------------------- Start / Stop

        public Task<Result<MirroringActionResponse>> StartMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId)
            => ChangeMirroringAsync(connectionId, workspaceId, mirroredDatabaseId, start: true);

        public Task<Result<MirroringActionResponse>> StopMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId)
            => ChangeMirroringAsync(connectionId, workspaceId, mirroredDatabaseId, start: false);

        private async Task<Result<MirroringActionResponse>> ChangeMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, bool start)
        {
            try
            {
                var context = await CreateContextAsync(connectionId);
                var action = start ? "start" : "stop";

                // Chặn sớm các thao tác thừa để trả lỗi rõ ràng thay vì lỗi chung từ Fabric
                var current = await GetMirroringStatusValueAsync(context, workspaceId, mirroredDatabaseId);
                if (start && current is Domain.Enums.MirroringStatus.Running or Domain.Enums.MirroringStatus.Starting)
                    return Result<MirroringActionResponse>.Failure(ResultStatus.Conflict, $"Mirroring is already {current}.");
                if (!start && current is Domain.Enums.MirroringStatus.Stopped or Domain.Enums.MirroringStatus.Stopping)
                    return Result<MirroringActionResponse>.Failure(ResultStatus.Conflict, $"Mirroring is already {current}.");

                var response = start
                    ? await context.Client.MirroredDatabase.Mirroring.StartMirroringAsync(workspaceId, mirroredDatabaseId)
                    : await context.Client.MirroredDatabase.Mirroring.StopMirroringAsync(workspaceId, mirroredDatabaseId);

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
                return Fail<MirroringActionResponse>(ex);
            }
        }

        // ---------------------------------------------------------------- Helpers

        private sealed class ServiceFailure(ResultStatus status, string message) : Exception(message)
        {
            public ResultStatus Status { get; } = status;
        }

        private async Task<FabricContext> CreateContextAsync(Guid connectionId)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId)
                ?? throw new ServiceFailure(ResultStatus.NotFound, "connection not found");

            return await fabricContextFactory.CreateFabricContextAsync(connection)
                ?? throw new ServiceFailure(ResultStatus.BadRequest, "can not create context");
        }

        private static Result<T> Fail<T>(Exception ex) => ex switch
        {
            ServiceFailure failure => Result<T>.Failure(failure.Status, failure.Message),
            AuthenticationFailedException => Result<T>.Failure(ResultStatus.BadRequest, ex.Message),
            RequestFailedException { Status: 404 } => Result<T>.Failure(ResultStatus.NotFound, "Mirrored database or workspace not found, or the connection can not access it."),
            RequestFailedException { Status: 401 or 403 } => Result<T>.Failure(ResultStatus.Forbidden, "The connection does not have access to this workspace."),
            RequestFailedException { Status: 400 } => Result<T>.Failure(ResultStatus.BadRequest, ex.Message),
            _ => Result<T>.Failure(ResultStatus.InternalError, ex.Message)
        };

        private async Task<Domain.Enums.MirroringStatus> GetMirroringStatusValueAsync(FabricContext context, Guid workspaceId, Guid mirroredDatabaseId)
        {
            try
            {
                var response = await context.Client.MirroredDatabase.Mirroring.GetMirroringStatusAsync(workspaceId, mirroredDatabaseId);
                return mapper.MapStatus(response.Value.Status);
            }
            catch (RequestFailedException ex)
            {
                logger.LogWarning(ex, "Cannot read mirroring status of {MirroredDatabaseId}.", mirroredDatabaseId);
                return Domain.Enums.MirroringStatus.Unknown;
            }
        }

        // mirroring.json cho biết nguồn và schema đích; definition là call nặng nên cache lại
        private async Task<MirroringDefinitionInfo?> GetDefinitionInfoAsync(FabricContext context, Guid workspaceId, Guid mirroredDatabaseId)
        {
            var cacheKey = cacheService.CreateCacheKey("mirrored-db-definition", workspaceId.ToString(), mirroredDatabaseId.ToString());
            var cached = await TryGetCachedDefinitionAsync(cacheKey);
            if (cached != null)
                return cached;

            try
            {
                var response = await context.Client.MirroredDatabase.Items.GetMirroredDatabaseDefinitionAsync(workspaceId, mirroredDatabaseId);
                var payload = response.Value.Definition?.Parts?
                    .FirstOrDefault(part => part.Path.Equals(MirroringDefinitionFileName, StringComparison.OrdinalIgnoreCase))?
                    .Payload;

                var info = MirroringDefinitionInfo.Parse(payload);
                if (info != null)
                    await TrySetCachedDefinitionAsync(cacheKey, info);
                return info;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Thiếu thông tin nguồn không nên làm hỏng cả danh sách
                logger.LogWarning(ex, "Cannot read definition of mirrored database {MirroredDatabaseId}.", mirroredDatabaseId);
                return null;
            }
        }

        // CacheService (Redis) chỉ lưu chuỗi nên phải tự serialize; lỗi cache không được làm hỏng request
        private async Task<MirroringDefinitionInfo?> TryGetCachedDefinitionAsync(string cacheKey)
        {
            try
            {
                var raw = (await cacheService.GetValueAsync(cacheKey))?.ToString();
                return string.IsNullOrEmpty(raw) ? null : JsonSerializer.Deserialize<MirroringDefinitionInfo>(raw);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot read mirroring definition from cache.");
                return null;
            }
        }

        private async Task TrySetCachedDefinitionAsync(string cacheKey, MirroringDefinitionInfo info)
        {
            try
            {
                await cacheService.SetValueAsync(cacheKey, JsonSerializer.Serialize(info), DefinitionCacheDuration);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot write mirroring definition to cache.");
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
