namespace FabricExplorerBackend.Infrastructures.OneLake.Dtos
{
    // Response of GET /common/v1/{ws}/{item}/schemas/{schema}/tables/{table}/additionalMetadata
    // (camelCase, so the default web serializer options are enough).
    public sealed record TableAdditionalMetadataDto(
        string? Description,
        IReadOnlyList<ColumnAdditionalMetadataDto>? Columns);

    public sealed record ColumnAdditionalMetadataDto(
        string? ColumnName,
        string? Description);
}
