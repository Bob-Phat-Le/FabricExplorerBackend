namespace FabricExplorerBackend.Application.QueryService
{
    /// <summary>One Parquet file of a result. <paramref name="StartRow"/> is the 0-based index of its first row.</summary>
    public sealed record QueryResultPart(string Name, long StartRow, long RowCount, long SizeBytes, int RowGroupCount);

    /// <summary>
    /// Describes the stored result (it is persisted as manifest.json next to the parts).
    /// Query lifecycle information (status, error, expiry) stays in <c>QueryMetadata</c>.
    /// </summary>
    public sealed record QueryResultMetadata(
        long RowCount,
        long TotalBytes,
        int PartCount,
        IReadOnlyList<QueryResultPart> Parts,
        bool IsTruncated,
        DateTimeOffset CreatedAt,
        DateTimeOffset CompletedAt,
        QueryResultSchema Schema);
}
