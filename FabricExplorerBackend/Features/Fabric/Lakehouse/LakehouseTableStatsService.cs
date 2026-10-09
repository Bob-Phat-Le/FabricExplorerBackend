using Azure.Core;
using Azure.Storage.Files.DataLake;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Cache;
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
        ISqlQueryService sqlQueryService,
        ICachedFetcher cachedFetcher) : ILakehouseTableStatsService
    {
        private const string OneLakeUrl = "https://onelake.dfs.fabric.microsoft.com";

        // Mọi thứ cần để đọc stats của các bảng trong một lakehouse (chuẩn bị một lần cho cả trang)
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
                    result[table.Name] = new TableStats(null, null, TableStatus.Unknown, "Không phải bảng Delta");
            }

            if (deltaTables.Count == 0) return result;

            // Stats (COUNT(*) qua SQL + đọc _delta_log) là phần tốn kém nhất của trang bảng nên cache theo TỪNG bảng:
            // mở lại / "Load more" / đổi page size chỉ tính lại những bảng chưa có hoặc đã hết hạn.
            var ttl = TimeSpan.FromSeconds(configuration.GetValue("Fabric:TableStatsCacheSeconds", 60));
            var maxParallelism = configuration.GetValue("Fabric:TableStatsMaxParallelism", 4);

            // Tra cứu lakehouse + token OneLake: làm đúng MỘT lần cho cả trang, và chỉ khi thật sự có bảng chưa có trong cache
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
                        // Kết quả lỗi thường là tạm thời (endpoint đang provision, mạng...) nên không được cache
                        cacheWhen: stats => stats.Status != TableStatus.Error,
                        cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Lỗi ở mức lakehouse/token/timeout: chỉ đánh dấu bảng này, không làm hỏng cả trang
                    result[name] = new TableStats(null, null, TableStatus.Error, ex.Message);
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
            // Việc chuẩn bị dùng chung cho nhiều bảng nên không gắn với token của request nào
            var lakehouse = await GetLakehouseAsync(connection, workspaceId, lakehouseId, CancellationToken.None);

            var sqlHost = lakehouse?.Properties?.SqlEndpointProperties?.ConnectionString;
            if (lakehouse == null || string.IsNullOrWhiteSpace(sqlHost))
                throw new InvalidOperationException("SQL analytics endpoint chưa sẵn sàng.");

            var provisioning = lakehouse.Properties?.SqlEndpointProperties?.ProvisioningStatus;
            if (!string.IsNullOrEmpty(provisioning) && !provisioning.Equals("Success", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"SQL analytics endpoint đang ở trạng thái '{provisioning}'.");

            // Token OneLake lấy ở đây; token SQL do SqlConnectionFactory tự lấy cho từng truy vấn
            var storageScope = configuration.GetValue("Scopes:storage", "https://storage.azure.com/.default")!;
            var storageToken = await tokenService.GetOrCreateTokenAsync(connection, storageScope);

            var fileSystem = new DataLakeServiceClient(new Uri(OneLakeUrl), new StaticTokenCredential(storageToken))
                .GetFileSystemClient(workspaceId.ToString());

            return new StatsSource(
                fileSystem,
                new SqlEndpoint(sqlHost, lakehouse.DisplayName),
                configuration.GetValue("Fabric:DefaultSqlSchema", "dbo")!);
        }

        private Task<FabricLakehouseDetailResponse?> GetLakehouseAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId, CancellationToken ct)
        {
            var client = fabricRestClientFactory.CreateFabricRestClient(connection);
            var url =
                $"{configuration.GetValue<string>("External:BaseFabricApiUrl")}" +
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