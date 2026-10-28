namespace FabricExplorerBackend.Application.Catalog.Contracts
{
    /// <summary>All schemas of a lakehouse together with the tables of each schema.</summary>
    public sealed record CatalogTreeResponse(
        Guid LakehouseId,
        int TotalTables,
        IReadOnlyList<CatalogSchemaResponse> Schemas);

    public sealed record CatalogSchemaResponse(
        string Name,
        IReadOnlyList<CatalogTableItemResponse> Tables);

    /// <param name="ColumnCount">Null when the table API did not include columns in the list response.</param>
    public sealed record CatalogTableItemResponse(
        string Name,
        string Schema,
        string? Type,
        string? Format,
        int? ColumnCount);
}
