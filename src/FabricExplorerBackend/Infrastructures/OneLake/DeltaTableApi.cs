using FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient;
using FabricExplorerBackend.Infrastructures.OneLake.Dtos;

namespace FabricExplorerBackend.Infrastructures.OneLake
{
    public sealed class DeltaTableApi(IFabricRestClientFactory clientFactory) : IDeltaTableApi
    {
        private const string TableApiHost = "https://onelake.table.fabric.microsoft.com";

        // Safety net against a misbehaving next_page_token loop.
        private const int MaxPages = 100;

        public async Task<IReadOnlyList<UnitySchemaDto>> ListSchemasAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId,
            CancellationToken cancellationToken = default)
        {
            var client = CreateClient(connection);
            var baseUrl = $"{UnityCatalogUrl(workspaceId, lakehouseId)}/schemas?catalog_name={lakehouseId}";

            var result = new List<UnitySchemaDto>();
            string? pageToken = null;
            for (var page = 0; page < MaxPages; page++)
            {
                var url = pageToken is null ? baseUrl : $"{baseUrl}&page_token={Uri.EscapeDataString(pageToken)}";
                var response = await client.GetAsync<UnityListSchemasResponse>(url, cancellationToken);

                if (response?.Schemas is { Count: > 0 } schemas)
                    result.AddRange(schemas);

                pageToken = response?.NextPageToken;
                if (string.IsNullOrEmpty(pageToken)) break;
            }

            return result;
        }

        public async Task<IReadOnlyList<UnityTableDto>> ListTablesAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId, string schemaName,
            CancellationToken cancellationToken = default)
        {
            var client = CreateClient(connection);
            var baseUrl =
                $"{UnityCatalogUrl(workspaceId, lakehouseId)}/tables" +
                $"?catalog_name={lakehouseId}&schema_name={Uri.EscapeDataString(schemaName)}";

            var result = new List<UnityTableDto>();
            string? pageToken = null;
            for (var page = 0; page < MaxPages; page++)
            {
                var url = pageToken is null ? baseUrl : $"{baseUrl}&page_token={Uri.EscapeDataString(pageToken)}";
                var response = await client.GetAsync<UnityListTablesResponse>(url, cancellationToken);

                if (response?.Tables is { Count: > 0 } tables)
                    result.AddRange(tables);

                pageToken = response?.NextPageToken;
                if (string.IsNullOrEmpty(pageToken)) break;
            }

            return result;
        }

        public Task<UnityTableDto?> GetTableAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId, string schemaName, string tableName,
            CancellationToken cancellationToken = default)
        {
            // Unity Catalog full name: {catalog}.{schema}.{table}, where catalog is the item (lakehouse).
            var fullName = Uri.EscapeDataString($"{lakehouseId}.{schemaName}.{tableName}");
            var url = $"{UnityCatalogUrl(workspaceId, lakehouseId)}/tables/{fullName}";

            return CreateClient(connection).GetAsync<UnityTableDto>(url, cancellationToken);
        }

        public Task<TableAdditionalMetadataDto?> GetTableAdditionalMetadataAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId, string schemaName, string tableName,
            CancellationToken cancellationToken = default)
        {
            var url =
                $"{TableApiHost}/common/v1/{workspaceId}/{lakehouseId}" +
                $"/schemas/{Uri.EscapeDataString(schemaName)}" +
                $"/tables/{Uri.EscapeDataString(tableName)}/additionalMetadata";

            return CreateClient(connection).GetAsync<TableAdditionalMetadataDto>(url, cancellationToken);
        }

        // OneLake table APIs only accept tokens issued for the Storage audience.
        private IFabricRestClient CreateClient(Domain.Entities.Connection connection)
            => clientFactory.CreateFabricRestClient(connection, FabricApiAudience.Storage);

        private static string UnityCatalogUrl(Guid workspaceId, Guid lakehouseId)
            => $"{TableApiHost}/delta/{workspaceId}/{lakehouseId}/api/2.1/unity-catalog";
    }
}
