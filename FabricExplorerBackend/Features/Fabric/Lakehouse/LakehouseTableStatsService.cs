using Azure.Core;
using Azure.Storage.Files.DataLake;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Fabric.FabricRestClient;
using FabricExplorerBackend.Features.Sql;
using FabricExplorerBackend.Features.Token;
using System.Collections.Concurrent;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public sealed class LakehouseTableStatsService(
        IConfiguration configuration,
        IFabricRestClientFactory fabricRestClientFactory,
        ITokenService tokenService,
        ISqlQueryService sqlQueryService) : ILakehouseTableStatsService
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
                var lakehouse = await GetLakehouseAsync(connection, lakehouseId, cancellationToken);

                var sqlHost = lakehouse?.Properties?.SqlEndpointProperties?.ConnectionString;
                if (lakehouse == null || string.IsNullOrWhiteSpace(sqlHost))
                    throw new InvalidOperationException("SQL analytics endpoint chưa sẵn sàng.");

                var provisioning = lakehouse.Properties?.SqlEndpointProperties?.ProvisioningStatus;
                if (!string.IsNullOrEmpty(provisioning) && !provisioning.Equals("Success", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"SQL analytics endpoint đang ở trạng thái '{provisioning}'.");

                // Token OneLake lấy ở đây; token SQL do SqlConnectionFactory tự lấy cho từng truy vấn
                var storageScope = configuration.GetValue("Scopes:storage", "https://storage.azure.com/.default")!;
                var storageToken = await tokenService.GetOrCreateTokenAsync(connection, storageScope);

                var sqlEndpoint = new SqlEndpoint(sqlHost, lakehouse.DisplayName);

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
                            fileSystem, connection, lakehouseId, sqlEndpoint, schema, name, cancellationToken);
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

            // Chạy song song 2 nguồn; lỗi nguồn này không làm mất kết quả nguồn kia
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
            // OneLake chấp nhận GUID cho cả workspace (file system) lẫn item.
            // Lakehouse bật schemas: đường dẫn sẽ là Tables/{schema}/{table}.
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

        // Phải là async: SqlIdentifier.Quote có thể ném ArgumentException, và lỗi đó cần nằm trong Task
        // để được bắt cùng các lỗi khác ở GetTableStatsAsync thay vì ném ra ngay khi gọi hàm
        private async Task<long?> CountRowsAsync(
            Domain.Entities.Connection connection,
            SqlEndpoint sqlEndpoint,
            string schema,
            string table,
            CancellationToken ct)
        {
            return await sqlQueryService.ExecuteScalarAsync<long?>(
                connection,
                sqlEndpoint,
                $"SELECT COUNT_BIG(*) FROM {SqlIdentifier.Quote(schema, table)}",
                cancellationToken: ct);
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