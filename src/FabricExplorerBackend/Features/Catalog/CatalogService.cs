using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Configurations.Options;
using FabricExplorerBackend.Features.Catalog.Contracts;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.OneLake;
using FabricExplorerBackend.Infrastructures.OneLake.Dtos;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Features.Catalog
{
    public sealed class CatalogService(
        ILogger<CatalogService> logger,
        IUnitOfWork unitOfWork,
        IDeltaTableApi deltaTableApi,
        ICachedFetcher cachedFetcher,
        IOptions<CatalogOptions> catalogOptions) : ICatalogService
    {
        private readonly CatalogOptions options = catalogOptions.Value;

        public async Task<Result<CatalogTreeResponse>> GetCatalogTreeAsync(
            Guid connectionId, Guid workspaceId, Guid lakehouseId,
            bool bypassCache = false, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection is null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var key = cachedFetcher.CreateKey(
                "catalog-tree", connectionId.ToString(), workspaceId.ToString(), lakehouseId.ToString());

            var tree = await cachedFetcher.GetOrFetchAsync(
                key,
                options.TreeCache,
                ct => BuildTreeAsync(connection, workspaceId, lakehouseId, ct),
                bypassCache,
                cancellationToken: cancellationToken);

            return Result<CatalogTreeResponse>.Success(tree);
        }

        public async Task<Result<CatalogTableDetailResponse>> GetTableDetailAsync(
            Guid connectionId, Guid workspaceId, Guid lakehouseId, string schemaName, string tableName,
            bool bypassCache = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(schemaName) || string.IsNullOrWhiteSpace(tableName))
                return Error.BadRequest(ErrorCodes.Catalog.BadRequest, "Schema name and table name are required.");

            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection is null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var key = cachedFetcher.CreateKey(
                "catalog-table", connectionId.ToString(), workspaceId.ToString(), lakehouseId.ToString(), schemaName, tableName);

            var detail = await cachedFetcher.GetOrFetchAsync<CatalogTableDetailResponse?>(
                key,
                options.TableDetailCache,
                ct => FetchTableDetailAsync(connection, workspaceId, lakehouseId, schemaName, tableName, ct),
                bypassCache,
                cancellationToken: cancellationToken);

            return detail is null
                ? Error.NotFound(ErrorCodes.Catalog.TableNotFound, $"Table '{schemaName}.{tableName}' was not found in this lakehouse.")
                : Result<CatalogTableDetailResponse>.Success(detail);
        }

        private async Task<CatalogTreeResponse> BuildTreeAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId, CancellationToken ct)
        {
            // Lakehouses without schema support are exposed by OneLake as a single fixed "dbo" schema.
            var schemas = await deltaTableApi.ListSchemasAsync(connection, workspaceId, lakehouseId, ct);

            using var gate = new SemaphoreSlim(Math.Max(1, options.SchemaParallelism));
            var loaded = await Task.WhenAll(schemas
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select(async schema =>
                {
                    await gate.WaitAsync(ct);
                    try
                    {
                        var tables = await deltaTableApi.ListTablesAsync(connection, workspaceId, lakehouseId, schema.Name, ct);
                        var items = tables
                            .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                            .Select(t => CatalogMapping.ToItem(t, schema.Name))
                            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        return new CatalogSchemaResponse(schema.Name, items);
                    }
                    finally
                    {
                        gate.Release();
                    }
                }));

            var ordered = loaded
                .OrderBy(s => string.Equals(s.Name, "dbo", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new CatalogTreeResponse(lakehouseId, ordered.Sum(s => s.Tables.Count), ordered);
        }

        private async Task<CatalogTableDetailResponse?> FetchTableDetailAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId,
            string schemaName, string tableName, CancellationToken ct)
        {
            // Both calls are independent, so run them together. Descriptions are optional:
            // a failure there must never hide the column structure.
            var tableTask = deltaTableApi.GetTableAsync(connection, workspaceId, lakehouseId, schemaName, tableName, ct);
            var additionalTask = TryGetAdditionalMetadataAsync(connection, workspaceId, lakehouseId, schemaName, tableName, ct);

            await Task.WhenAll(tableTask, additionalTask);

            var table = await tableTask;
            return table is null
                ? null
                : CatalogMapping.ToDetail(table, schemaName, tableName, await additionalTask);
        }

        private async Task<TableAdditionalMetadataDto?> TryGetAdditionalMetadataAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId,
            string schemaName, string tableName, CancellationToken ct)
        {
            try
            {
                return await deltaTableApi.GetTableAdditionalMetadataAsync(
                    connection, workspaceId, lakehouseId, schemaName, tableName, ct);
            }
            catch (ExternalServiceException ex)
            {
                logger.LogWarning(ex,
                    "Could not read additional metadata of {Schema}.{Table} in lakehouse {LakehouseId}; descriptions are skipped.",
                    schemaName, tableName, lakehouseId);
                return null;
            }
        }
    }
}
