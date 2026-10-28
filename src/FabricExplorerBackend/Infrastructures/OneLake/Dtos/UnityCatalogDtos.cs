using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Infrastructures.OneLake.Dtos
{
    // OneLake Delta Table API speaks the Unity Catalog REST dialect: snake_case properties
    // and list responses wrapped in an object ({ "schemas": [...] } / { "tables": [...] }).
    // JsonPropertyName is used instead of changing the global serializer options of FabricRestClient.

    public sealed record UnityListSchemasResponse(
        [property: JsonPropertyName("schemas")] IReadOnlyList<UnitySchemaDto>? Schemas,
        [property: JsonPropertyName("next_page_token")] string? NextPageToken);

    public sealed record UnityListTablesResponse(
        [property: JsonPropertyName("tables")] IReadOnlyList<UnityTableDto>? Tables,
        [property: JsonPropertyName("next_page_token")] string? NextPageToken);

    public sealed record UnitySchemaDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("catalog_name")] string? CatalogName,
        [property: JsonPropertyName("full_name")] string? FullName,
        [property: JsonPropertyName("schema_id")] string? SchemaId);

    public sealed record UnityTableDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("catalog_name")] string? CatalogName,
        [property: JsonPropertyName("schema_name")] string? SchemaName,
        [property: JsonPropertyName("table_id")] string? TableId,
        [property: JsonPropertyName("table_type")] string? TableType,
        [property: JsonPropertyName("data_source_format")] string? DataSourceFormat,
        [property: JsonPropertyName("storage_location")] string? StorageLocation,
        [property: JsonPropertyName("comment")] string? Comment,
        [property: JsonPropertyName("columns")] IReadOnlyList<UnityColumnDto>? Columns);

    public sealed record UnityColumnDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("type_text")] string? TypeText,
        [property: JsonPropertyName("type_name")] string? TypeName,
        [property: JsonPropertyName("position")] int? Position,
        [property: JsonPropertyName("nullable")] bool? Nullable,
        [property: JsonPropertyName("comment")] string? Comment);
}
