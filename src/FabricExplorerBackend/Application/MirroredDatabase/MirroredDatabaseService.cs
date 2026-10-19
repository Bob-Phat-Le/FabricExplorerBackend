using Azure;
using FabricExplorerBackend.Application.MirroredDatabase.Contracts;
using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;
using FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Application.MirroredDatabase
{
    public class MirroredDatabaseService(
        IMirroredDatabaseMapper mapper,
        IFabricContextFactory fabricContextFactory,
        IFabricRestClientFactory fabricRestClientFactory,
        IFabricOperationMapper fabricOperationMapper,
        IFabricOperationService fabricOperationService,
        ICachedFetcher cachedFetcher,
        IOptions<CacheOptions> cacheOptions,
        IOptions<FabricOptions> fabricOptions,
        IUnitOfWork unitOfWork,
        ILogger<MirroredDatabaseService> logger) : IMirroredDatabaseService
    {
        private const string MirroringDefinitionFileName = "mirroring.json";
        private CacheOptions cacheOptions = cacheOptions.Value;
        private FabricOptions fabricOptions = fabricOptions.Value;

        public async Task<Result<IEnumerable<MirroredDatabaseListItemResponse>>> ListMirroredDatabasesAsync(
            Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var ttl = cacheOptions.ItemList;
            var key = cachedFetcher.CreateKey("mirrored-dbs", connectionId.ToString(), workspaceId.ToString());

            var items = await cachedFetcher.GetOrFetchAsync<IReadOnlyList<MirroredDatabaseListItemResponse>>(
                key, ttl,
                async ct =>
                {
                    var context = await CreateContextAsync(connection, ct);
                    //context = await fabricContextFactory.CreateFabricContextWithoutTokenAsync(connection, cancellationToken);
                    //if (context == null)
                    //    return Error.BadRequest(ErrorCodes.Context.BadRequest, "Can not create context");

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

        public async Task<Result<MirroredDatabaseDetailResponse>> GetMirroredDatabaseByIdAsync(
            Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var context = await CreateContextAsync(connection, cancellationToken);

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

        public async Task<Result<MirroringDatabaseStatusResponse>> GetMirroringStatusAsync(
            Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var context = await CreateContextAsync(connection, cancellationToken);

            var statusTask = context.Client.MirroredDatabase.Mirroring.GetMirroringStatusAsync(workspaceId, mirroredDatabaseId, cancellationToken: cancellationToken);

            var tablesTask = ListOrEmptyOnRejectedAsync(
                GetTablesMirroringStatusAsync(connection, workspaceId, mirroredDatabaseId, cancellationToken));

            var mirroringStatus = (await statusTask).Value;

            var tables = await tablesTask;

            var response = mapper.Map(mirroringStatus);
            response.LastSynchronization = tables.Select(item => item.Metrics?.LastSyncDateTime).Max();
            response.RecordSynchronized = tables.Sum(item => item.Metrics?.ProcessedRows ?? 0);
            response.CurrentLatency = tables.Select(item => item.Metrics?.LastSyncLatencyInSeconds).Max();

            return Result<MirroringDatabaseStatusResponse>.Success(response);
        }

        public async Task<Result<IEnumerable<TableMirroringStatusResponse>>> ListTablesMirroringStatusAsync(
            Guid connectionId,
            Guid workspaceId,
            Guid mirroredDatabaseId,
            CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var context = await CreateContextAsync(connection, cancellationToken);

            var tablesTask = GetTablesMirroringStatusAsync(connection, workspaceId, mirroredDatabaseId, cancellationToken);
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

            return Result<IEnumerable<TableMirroringStatusResponse>>.Success(response);
        }

        public Task<Result<MirroringActionResponse>> StartMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default)
            => ChangeMirroringAsync(connectionId, workspaceId, mirroredDatabaseId, start: true, cancellationToken);

        public Task<Result<MirroringActionResponse>> StopMirroringAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken = default)
            => ChangeMirroringAsync(connectionId, workspaceId, mirroredDatabaseId, start: false, cancellationToken);

        private async Task<Result<MirroringActionResponse>> ChangeMirroringAsync(
            Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, bool start, CancellationToken cancellationToken)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var context = await CreateContextAsync(connection, cancellationToken);

            var action = start ? "start" : "stop";

            var current = await GetMirroringStatusValueAsync(context, workspaceId, mirroredDatabaseId, cancellationToken);
            if (start && current is MirroringStatus.Running or MirroringStatus.Starting)
                return Error.Conflict(ErrorCodes.Fabric.Conflict, $"Mirroring is already {current}.");
            if (!start && current is MirroringStatus.Stopped or MirroringStatus.Stopping)
                return Error.Conflict(ErrorCodes.Fabric.Conflict, $"Mirroring is already {current}.");

            var response = start
                ? await context.Client.MirroredDatabase.Mirroring.StartMirroringAsync(workspaceId, mirroredDatabaseId, cancellationToken: CancellationToken.None)
                : await context.Client.MirroredDatabase.Mirroring.StopMirroringAsync(workspaceId, mirroredDatabaseId, cancellationToken: CancellationToken.None);

            var result = new MirroringActionResponse
            {
                Message = start
                    ? "Start mirroring requested. Poll the status endpoint until it becomes Running."
                    : "Stop mirroring requested. Poll the status endpoint until it becomes Stopped."
            };

            if (response.Status == StatusCodes.Status202Accepted)
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

            return Result<MirroringActionResponse>.Success(result);
        }

        private sealed class ServiceFailure(ErrorKind status, string message) : Exception(message)
        {
            public ErrorKind Status { get; } = status;
        }

        private async Task<FabricContext> CreateContextAsync(Domain.Entities.Connection connection, CancellationToken cancellationToken)
            => await fabricContextFactory.CreateFabricContextWithoutTokenAsync(connection, cancellationToken)
                ?? throw new ServiceFailure(ErrorKind.BadRequest, "can not create context");

        private async Task<MirroringStatus> GetMirroringStatusValueAsync(
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
                return MirroringStatus.Unknown;
            }
        }

        private async Task<MirroringDefinitionInfo?> GetDefinitionInfoAsync(
            FabricContext context, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken)
        {
            var ttl = cacheOptions.MirroringDefinition;
            var key = cachedFetcher.CreateKey("mirrored-db-definition", workspaceId.ToString(), mirroredDatabaseId.ToString());

            try
            {
                var item = await cachedFetcher.GetOrFetchAsync<MirroringDefinitionInfo?>(
                    key, ttl,
                    async ct =>
                    {
                        var response = await context.Client.MirroredDatabase.Items
                            .GetMirroredDatabaseDefinitionAsync(workspaceId, mirroredDatabaseId, cancellationToken: ct);
                        var payload = response.Value.Definition?.Parts?
                            .FirstOrDefault(part => part.Path.Equals(MirroringDefinitionFileName, StringComparison.OrdinalIgnoreCase))?
                            .Payload;

                        return MirroringDefinitionInfo.Parse(payload);
                    },
                    cancellationToken: cancellationToken);
                return item;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot read definition of mirrored database {MirroredDatabaseId}.", mirroredDatabaseId);
                return null;
            }
        }

        private async Task<IReadOnlyList<T>> ListOrEmptyOnRejectedAsync<T>(Task<IReadOnlyList<T>> task)
        {
            try
            {
                return await task;
            }
            catch (ExternalServiceException ex) when (ex.UpstreamStatus is 400 or 409)
            {
                logger.LogWarning(ex, "Fabric rejected the tables mirroring status request ({Status}); treating it as empty.", ex.UpstreamStatus);
                return [];
            }
        }

        private const int MaxStatusPages = 100;

        private async Task<IReadOnlyList<TableMirroringStatusDto>> GetTablesMirroringStatusAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid mirroredDatabaseId, CancellationToken cancellationToken)
        {
            var client = fabricRestClientFactory.CreateFabricRestClient(connection);
            var url = $"{fabricOptions.Api.BaseUrl.TrimEnd('/')}/workspaces/{workspaceId}/mirroredDatabases/{mirroredDatabaseId}/getTablesMirroringStatus";

            var items = new List<TableMirroringStatusDto>();
            string? continuationToken = null;

            for (var page = 0; page < MaxStatusPages; page++)
            {
                var pageUrl = continuationToken is null
                    ? url
                    : $"{url}?continuationToken={Uri.EscapeDataString(continuationToken)}";

                var response = await client.PostAsync<object?, FabricValueListResponse<TableMirroringStatusDto>>(
                    pageUrl, null, cancellationToken);

                if (response?.Data is { } data)
                    items.AddRange(data.OfType<TableMirroringStatusDto>());

                continuationToken = response?.ContinuationToken is { Length: > 0 } next ? next : null;
                if (continuationToken is null)
                    return items;
            }

            logger.LogWarning(
                "Stopped reading tables mirroring status of {MirroredDatabaseId} after {Pages} pages.",
                mirroredDatabaseId, MaxStatusPages);
            return items;
        }
    }
}
