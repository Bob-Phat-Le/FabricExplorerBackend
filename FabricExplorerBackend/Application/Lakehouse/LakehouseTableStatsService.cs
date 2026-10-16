using Azure.Core;
using Azure.Storage.Files.DataLake;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;
using FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient;
using FabricExplorerBackend.Infrastructures.Identity;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Sql;
using Microsoft.Extensions.Options;
using Serilog.Core;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace FabricExplorerBackend.Application.Lakehouse
{
    public sealed class LakehouseTableStatsService(
        IOptions<FabricOptions> fabricOptions,
        IOptions<CacheOptions> cacheOptions,
        IOptions<OperationPollingOptions> operationPollingOptions,
        IOptions<AzureOptions> azureOptions,
        IFabricRestClientFactory fabricRestClientFactory,
        ITokenService tokenService,
        ISqlQueryService sqlQueryService,
        ILogger<LakehouseTableStatsService> logger,
        ICachedFetcher cachedFetcher) : ILakehouseTableStatsService
    {
        private const string OneLakeUrl = "https://onelake.dfs.fabric.microsoft.com";
        private FabricOptions fabricOptions = fabricOptions.Value;
        private CacheOptions cacheOptions = cacheOptions.Value;
        private OperationPollingOptions operationPollingOptions = operationPollingOptions.Value;
        private AzureOptions azureOptions = azureOptions.Value;
        private sealed record StatsSource(DataLakeFileSystemClient FileSystem, SqlEndpoint SqlEndpoint, string Schema);

        public async Task<IReadOnlyDictionary<string, TableStats>> GetStatsAsync(
            Domain.Entities.Connection connection,
            Guid workspaceId,
            Guid lakehouseId,
            IEnumerable<FabricTableResponse> tables,
            bool bypassCache = false,
            CancellationToken cancellationToken = default)
        {
            var result = new ConcurrentDictionary<string, TableStats>(StringComparer.OrdinalIgnoreCase);
            var deltaTables = new List<string>();

            foreach (var table in tables)
            {
                if (string.Equals(table.Format, "delta", StringComparison.OrdinalIgnoreCase))
                    deltaTables.Add(table.Name);
                else
                    result[table.Name] = new TableStats(null, null, TableStatus.Unknown, "Not delta table");
            }

            if (deltaTables.Count == 0) return result;

            var ttl = cacheOptions.TableStats;
            var maxParallelism = operationPollingOptions.TableStatsMaxParallelism;

            var source = new Lazy<Task<StatsSource>>(() => PrepareSourceAsync(connection, workspaceId, lakehouseId));
            using var gate = new SemaphoreSlim(maxParallelism);

            await Task.WhenAll(deltaTables.Select(async name =>
            {
                await gate.WaitAsync(cancellationToken);

                try
                {
                    var key = cachedFetcher.CreateKey(
                        "table-stats", connection.Id.ToString(), workspaceId.ToString(), lakehouseId.ToString(), name);

                    result[name] = await cachedFetcher.GetOrFetchAsync(
                        key, ttl,
                        async ct =>
                        {
                            var src = await source.Value;
                            return await GetTableStatsAsync(
                                src.FileSystem, connection, lakehouseId, src.SqlEndpoint, src.Schema, name, ct);
                        },
                        bypassCache,
                        cacheWhen: stats => stats.Status != TableStatus.Error,
                        cancellationToken: cancellationToken);
                }
                finally
                {
                    gate.Release();
                }
            }));
            return result;
        }

        private async Task<StatsSource> PrepareSourceAsync(Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId)
        {
            var lakehouse = await GetLakehouseAsync(connection, workspaceId, lakehouseId, CancellationToken.None);

            var sqlHost = lakehouse?.Properties?.SqlEndpointProperties?.ConnectionString;
            if (lakehouse == null || string.IsNullOrWhiteSpace(sqlHost))
                throw new InvalidOperationException("SQL analytics endpoint not ready");

            var provisioning = lakehouse.Properties?.SqlEndpointProperties?.ProvisioningStatus;
            if (!string.IsNullOrEmpty(provisioning) && !provisioning.Equals("Success", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"SQL analytics endpoint is at state '{provisioning}'.");

            var storageScope = azureOptions.Scopes.Storage;
            var storageToken = await tokenService.GetOrCreateTokenAsync(connection, storageScope);
            var credential = tokenService.CreateClientSecretCredential(connection.TenantId, connection.ClientId, connection.ClientSecret);

            var fileSystem = new DataLakeServiceClient(new Uri(OneLakeUrl), credential)
                .GetFileSystemClient(workspaceId.ToString());

            return new StatsSource(
                fileSystem,
                new SqlEndpoint(sqlHost, lakehouse.DisplayName),
                fabricOptions.SqlSchemas.Default);
        }

        private Task<FabricLakehouseDetailResponse?> GetLakehouseAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId, CancellationToken ct)
        {
            var client = fabricRestClientFactory.CreateFabricRestClient(connection);
            var url =
                $"{fabricOptions.Api.BaseUrl}" +
                $"/workspaces/{workspaceId}/lakehouses/{lakehouseId}";
            return client.GetAsync<FabricLakehouseDetailResponse>(url, ct);
        }

        private async Task<TableStats> GetTableStatsAsync(
            DataLakeFileSystemClient fileSystem,
            Domain.Entities.Connection connection,
            Guid lakehouseId,
            SqlEndpoint sqlEndpoint,
            string schema,
            string table,
            CancellationToken ct)
        {
            var errors = new List<string>();
            long? rows = null;
            DateTimeOffset? lastModified = null;

            var lastModifiedTask = GetLastCommitTimeAsync(fileSystem, lakehouseId, table, ct);
            var rowsTask = CountRowsAsync(connection, sqlEndpoint, schema, table, ct);

            try { lastModified = await lastModifiedTask; }
            catch (Exception ex) { errors.Add($"LastModified: {ex.Message}"); }

            try { rows = await rowsTask; }
            catch (Exception ex) { errors.Add($"RowCount: {ex.Message}"); }

            var (status, message) = Evaluate(rows, lastModified, errors);
            return new TableStats(rows, lastModified, status, message);
        }

        private static async Task<DateTimeOffset?> GetLastCommitTimeAsync(
            DataLakeFileSystemClient fileSystem, Guid lakehouseId, string table, CancellationToken ct)
        {
            var directory = $"{lakehouseId}/Tables/{table}/_delta_log";
            DateTimeOffset? latest = null;

            await foreach (var path in fileSystem.GetPathsAsync(
                               path: directory,
                               recursive: false,
                               userPrincipalName: false,
                               cancellationToken: ct))
            {
                if (path.IsDirectory == true || !path.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (latest is null || path.LastModified > latest)
                    latest = path.LastModified;
            }

            return latest;
        }

        private async Task<long?> CountRowsAsync(
            Domain.Entities.Connection connection,
            SqlEndpoint sqlEndpoint,
            string schema,
            string table,
            CancellationToken ct)
        {
            var result = await sqlQueryService.ExecuteScalarAsync<long?>(
                connection,
                sqlEndpoint,
                $"SELECT COUNT_BIG(*) FROM {SqlIdentifier.Quote(schema, table)}",
                cancellationToken: ct);

            return result;
        }

        private (TableStatus Status, string? Message) Evaluate(
            long? rows, DateTimeOffset? lastModified, IReadOnlyCollection<string> errors)
        {
            if (errors.Count > 0)
                return (TableStatus.Error, string.Join(" | ", errors));

            if (rows is null || lastModified is null)
                return (TableStatus.Unknown, "Missing data to evaluate");

            if (rows == 0)
                return (TableStatus.Empty, null);

            var staleAfterDays = cacheOptions.TableStale;
            if (DateTimeOffset.UtcNow - lastModified > staleAfterDays)
                return (TableStatus.Stale, $"No update more than {staleAfterDays} ngày");

            return (TableStatus.Healthy, null);
        }

        //private sealed class StaticTokenCredential(string token) : TokenCredential
        //{
        //    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        //        => new(token, DateTimeOffset.UtcNow.AddMinutes(5));

        //    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        //        => new(GetToken(requestContext, cancellationToken));
        //}
    }
}