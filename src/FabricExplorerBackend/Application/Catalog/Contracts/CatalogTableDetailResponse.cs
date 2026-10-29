namespace FabricExplorerBackend.Application.Catalog.Contracts
{
    public sealed record CatalogTableDetailResponse(
        string Schema,
        string Name,
        string FullName,
        string? Description,
        string? Type,
        string? Format,
        string? Location,
        IReadOnlyList<CatalogColumnResponse> Columns);

    public sealed record CatalogColumnResponse(
        string Name,
        string Type,
        int Position,
        string PrimaryKey,
        bool Nullable,
        string? Description);
}
