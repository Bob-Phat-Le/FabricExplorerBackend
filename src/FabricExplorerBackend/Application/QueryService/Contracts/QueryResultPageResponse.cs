namespace FabricExplorerBackend.Application.QueryService.Contracts
{
    public sealed record QueryResultPageResponse(
        Guid QueryId,
        long Offset,
        int Limit,
        long TotalRows,
        bool HasMore,
        bool IsTruncated,
        IReadOnlyList<QueryResultColumn> Columns,
        IReadOnlyList<object?[]> Rows);
}
