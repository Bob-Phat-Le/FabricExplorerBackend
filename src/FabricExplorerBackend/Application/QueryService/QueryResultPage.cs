namespace FabricExplorerBackend.Application.QueryService
{
    /// <summary>A window of rows. Each row is aligned with <see cref="Columns"/>.</summary>
    public sealed record QueryResultPage(
        long TotalRows,
        bool IsTruncated,
        IReadOnlyList<QueryResultColumn> Columns,
        IReadOnlyList<object?[]> Rows);
}
