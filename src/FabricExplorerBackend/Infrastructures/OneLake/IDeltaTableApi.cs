using FabricExplorerBackend.Infrastructures.OneLake.Dtos;

namespace FabricExplorerBackend.Infrastructures.OneLake
{
    /// <summary>
    /// Thin client over the OneLake table APIs (https://onelake.table.fabric.microsoft.com).
    /// Only transport concerns live here: URL building, paging and token audience.
    /// Caching and mapping to API contracts belong to the application layer.
    /// </summary>
    public interface IDeltaTableApi
    {
        Task<IReadOnlyList<UnitySchemaDto>> ListSchemasAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UnityTableDto>> ListTablesAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId, string schemaName,
            CancellationToken cancellationToken = default);

        Task<UnityTableDto?> GetTableAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId, string schemaName, string tableName,
            CancellationToken cancellationToken = default);

        Task<TableAdditionalMetadataDto?> GetTableAdditionalMetadataAsync(
            Domain.Entities.Connection connection, Guid workspaceId, Guid lakehouseId, string schemaName, string tableName,
            CancellationToken cancellationToken = default);
    }
}
