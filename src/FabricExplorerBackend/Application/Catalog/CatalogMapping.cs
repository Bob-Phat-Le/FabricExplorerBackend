using FabricExplorerBackend.Application.Catalog.Contracts;
using FabricExplorerBackend.Infrastructures.OneLake.Dtos;

namespace FabricExplorerBackend.Application.Catalog
{
    internal static class CatalogMapping
    {
        public static CatalogTableItemResponse ToItem(UnityTableDto table, string schemaName)
            => new(
                table.Name,
                schemaName,
                table.TableType,
                table.DataSourceFormat,
                table.Columns?.Count);

        public static CatalogTableDetailResponse ToDetail(
            UnityTableDto table,
            string schemaName,
            string tableName,
            TableAdditionalMetadataDto? additional)
        {
            // OneLake additional metadata is the source of truth for descriptions;
            // the Unity Catalog "comment" is only used as a fallback.
            var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in additional?.Columns ?? [])
            {
                if (!string.IsNullOrWhiteSpace(column.ColumnName) && !string.IsNullOrWhiteSpace(column.Description))
                    descriptions.TryAdd(column.ColumnName, column.Description.Trim());
            }

            var columns = (table.Columns ?? [])
                .Select((c, index) => new CatalogColumnResponse(
                    c.Name,
                    FirstNonEmpty(c.TypeText, c.TypeName) ?? "unknown",
                    c.Position ?? index,
                    "Unsupported",
                    c.Nullable ?? true,
                    descriptions.TryGetValue(c.Name, out var description)
                        ? description
                        : NullIfBlank(c.Comment)))
                .OrderBy(c => c.Position)
                .ToList();

            return new CatalogTableDetailResponse(
                schemaName,
                table.Name ?? tableName,
                $"{schemaName}.{table.Name ?? tableName}",
                NullIfBlank(additional?.Description) ?? NullIfBlank(table.Comment),
                table.TableType,
                table.DataSourceFormat,
                table.StorageLocation,
                columns);
        }

        private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }
}
