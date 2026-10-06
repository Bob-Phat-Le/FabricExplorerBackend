using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Fabric.FabricRestClient;
using FabricExplorerBackend.Features.Token;
using System.Collections.Concurrent;
using Azure.Storage.Files.DataLake;
using Microsoft.Data.SqlClient;
using Azure.Core;
using FabricExplorerBackend.Features.Fabric.Context;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public sealed class LakehouseTableStatsService(
        IConfiguration configuration,
        IFabricContextFactory fabricContextFactory,
        IFabricRestClientFactory fabricRestClientFactory,
        ITokenService tokenService) : ILakehouseTableStatsService
    {
        private const string OneLakeUrl = "https://onelake.dfs.fabric.microsoft.com";

        public async Task<IReadOnlyDictionary<string, TableStats>> GetStatsAsync(
            Domain.Entities.Connection connection,
            Guid lakehouseId,
            IEnumerable<FabricTableResponse> tables,
            CancellationToken cancellationToken = default)
        {
            var result = new ConcurrentDictionary<string, TableStats>(StringComparer.OrdinalIgnoreCase);
            var deltaTables = new List<string>();

            foreach (var table in tables)
            {
                if (string.Equals(table.Format, "delta", StringComparison.OrdinalIgnoreCase))
                    deltaTables.Add(table.Name);
                else
                    result[table.Name] = new TableStats(null, null, TableStatus.Unknown, "Không phải bảng Delta");
            }

            if (deltaTables.Count == 0) return result;

            try
            {
                //var lakehouse = await GetLakehouseAsync(connection, lakehouseId, cancellationToken);

                //var sqlEndpoint = lakehouse?.Properties?.SqlEndpointProperties?.ConnectionString;
                //if (lakehouse == null || string.IsNullOrWhiteSpace(sqlEndpoint))
                //    throw new InvalidOperationException("SQL analytics endpoint chưa sẵn sàng.");

                //var provisioning = lakehouse.Properties?.SqlEndpointProperties?.ProvisioningStatus;
                //if (!string.IsNullOrEmpty(provisioning) && !provisioning.Equals("Success", StringComparison.OrdinalIgnoreCase))
                //    throw new InvalidOperationException($"SQL analytics endpoint đang ở trạng thái '{provisioning}'.");

                //// Mỗi dịch vụ cần một token với audience riêng (TokenService cache theo scope)
                //var storageScope = configuration.GetValue("Scopes:storage", "https://storage.azure.com/.default")!;
                //var sqlScope = configuration.GetValue("Scopes:sql", "https://database.windows.net/.default")!;
                //var storageToken = await tokenService.GetOrCreateTokenAsync(connection, storageScope);
                //var sqlToken = await tokenService.GetOrCreateTokenAsync(connection, sqlScope);

                //var fileSystem = new DataLakeServiceClient(new Uri(OneLakeUrl), new StaticTokenCredential(storageToken))
                //    .GetFileSystemClient(connection.WorkspaceId.ToString());

                //var schema = configuration.GetValue("Fabric:DefaultSqlSchema", "dbo")!;
                //var maxParallelism = configuration.GetValue("Fabric:TableStatsMaxParallelism", 4);
                //using var gate = new SemaphoreSlim(maxParallelism);

                //await Task.WhenAll(deltaTables.Select(async name =>
                //{
                //    await gate.WaitAsync(cancellationToken);
                //    try
                //    {
                //        result[name] = await GetTableStatsAsync(
                //            fileSystem, lakehouseId, sqlEndpoint, lakehouse.DisplayName, schema, sqlToken, name, cancellationToken);
                //    }
                //    finally
                //    {
                //        gate.Release();
                //    }
                //}));

                var context = await fabricContextFactory.CreateFabricContextAsync(connection);
                var response = await context!.Client.Lakehouse.Items.GetLakehouseAsync(connection.WorkspaceId, lakehouseId, cancellationToken);
                var lakehouse = response.Value;

                var sqlEndpoint = lakehouse?.Properties?.SqlEndpointProperties?.ConnectionString;
                if (lakehouse == null || string.IsNullOrWhiteSpace(sqlEndpoint))
                    throw new InvalidOperationException("SQL analytics endpoint chưa sẵn sàng.");

                var provisioning = lakehouse.Properties?.SqlEndpointProperties?.ProvisioningStatus;
                if (provisioning.HasValue && !provisioning.Value.Equals(SqlEndpointProvisioningStatus.Success))
                    throw new InvalidOperationException($"SQL analytics endpoint đang ở trạng thái '{provisioning}'.");

                // Mỗi dịch vụ cần một token với audience riêng (TokenService cache theo scope)
                var storageScope = configuration.GetValue("Scopes:storage", "https://storage.azure.com/.default")!;
                var sqlScope = configuration.GetValue("Scopes:sql", "https://database.windows.net/.default")!;
                var storageToken = await tokenService.GetOrCreateTokenAsync(connection, storageScope);
                var sqlToken = await tokenService.GetOrCreateTokenAsync(connection, sqlScope);

                var fileSystem = new DataLakeServiceClient(new Uri(OneLakeUrl), new StaticTokenCredential(storageToken))
                    .GetFileSystemClient(connection.WorkspaceId.ToString());

                var schema = configuration.GetValue("Fabric:DefaultSqlSchema", "dbo")!;
                var maxParallelism = configuration.GetValue("Fabric:TableStatsMaxParallelism", 4);
                using var gate = new SemaphoreSlim(maxParallelism);

                await Task.WhenAll(deltaTables.Select(async name =>
                {
                    await gate.WaitAsync(cancellationToken);
                    try
                    {
                        result[name] = await GetTableStatsAsync(
                            fileSystem, lakehouseId, sqlEndpoint, lakehouse.DisplayName, schema, sqlToken, name, cancellationToken);
                    }
                    finally
                    {
                        gate.Release();
                    }
                }));
            }
            catch (Exception ex)
            {
                // Lỗi ở mức lakehouse/token: đánh dấu các bảng Delta chưa có kết quả
                foreach (var name in deltaTables)
                    result.TryAdd(name, new TableStats(null, null, TableStatus.Error, ex.Message));
            }

            return result;
        }

        private Task<FabricLakehouseDetailResponse?> GetLakehouseAsync(
            Domain.Entities.Connection connection, Guid lakehouseId, CancellationToken ct)
        {
            var client = fabricRestClientFactory.CreateFabricRestClient(connection);
            var url =
                $"{configuration.GetValue<string>("External:BaseFabricApiUrl")}" +
                $"/workspaces/{connection.WorkspaceId}/lakehouses/{lakehouseId}";
            return client.GetAsync<FabricLakehouseDetailResponse>(url, ct);
        }

        private async Task<TableStats> GetTableStatsAsync(
            DataLakeFileSystemClient fileSystem,
            Guid lakehouseId,
            string sqlEndpoint,
            string database,
            string schema,
            string sqlToken,
            string table,
            CancellationToken ct)
        {
            var errors = new List<string>();
            long? rows = null;
            DateTimeOffset? lastModified = null;

            // Chạy song song 2 nguồn; lỗi nguồn này không làm mất kết quả nguồn kia
            var lastModifiedTask = GetLastCommitTimeAsync(fileSystem, lakehouseId, table, ct);
            var rowsTask = CountRowsAsync(sqlEndpoint, database, schema, sqlToken, table, ct);

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
            // OneLake chấp nhận GUID cho cả workspace (file system) lẫn item.
            // Lakehouse bật schemas: đường dẫn sẽ là Tables/{schema}/{table}.
            var directory = $"{lakehouseId}/Tables/{table}/_delta_log";
            DateTimeOffset? latest = null;

            await foreach (var path in fileSystem.GetPathsAsync(path: directory, recursive: false, userPrincipalName: false, cancellationToken: ct))
            {
                if (path.IsDirectory == true || !path.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (latest is null || path.LastModified > latest)
                    latest = path.LastModified;
            }

            return latest;
        }

        private static async Task<long?> CountRowsAsync(
            string sqlEndpoint, string database, string schema, string accessToken, string table, CancellationToken ct)
        {
            var connectionString = $"Server={sqlEndpoint};Database={database};Encrypt=True;Connect Timeout=30";

            await using var sqlConnection = new SqlConnection(connectionString) { AccessToken = accessToken };
            await sqlConnection.OpenAsync(ct);

            var safeSchema = schema.Replace("]", "]]");
            var safeTable = table.Replace("]", "]]");

            await using var command = new SqlCommand(
                $"SELECT COUNT_BIG(*) FROM [{safeSchema}].[{safeTable}]", sqlConnection)
            {
                CommandTimeout = 60
            };

            var scalar = await command.ExecuteScalarAsync(ct);
            return scalar is null or DBNull ? null : Convert.ToInt64(scalar);
        }

        private (TableStatus Status, string? Message) Evaluate(
            long? rows, DateTimeOffset? lastModified, IReadOnlyCollection<string> errors)
        {
            if (errors.Count > 0)
                return (TableStatus.Error, string.Join(" | ", errors));

            if (rows is null || lastModified is null)
                return (TableStatus.Unknown, "Thiếu dữ liệu để đánh giá");

            if (rows == 0)
                return (TableStatus.Empty, null);

            var staleAfterDays = configuration.GetValue("Fabric:TableStaleAfterDays", 30);
            if (DateTimeOffset.UtcNow - lastModified > TimeSpan.FromDays(staleAfterDays))
                return (TableStatus.Stale, $"Không cập nhật hơn {staleAfterDays} ngày");

            return (TableStatus.Healthy, null);
        }

        // TokenService của dự án trả về chuỗi token, còn DataLakeServiceClient cần TokenCredential
        private sealed class StaticTokenCredential(string token) : TokenCredential
        {
            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
                => new(token, DateTimeOffset.UtcNow.AddMinutes(5));

            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
                => new(GetToken(requestContext, cancellationToken));
        }
    }
}
