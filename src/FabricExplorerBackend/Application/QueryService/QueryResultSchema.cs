namespace FabricExplorerBackend.Application.QueryService
{
    /// <summary>
    /// One column of a stored result.
    /// <paramref name="DataType"/> is the logical type of the stored (Parquet) data:
    /// boolean, byte, int16, int32, int64, float, double, decimal, datetime, date, guid, binary, string.
    /// <paramref name="SqlType"/> is the type name reported by the SQL endpoint.
    /// </summary>
    public sealed record QueryResultColumn(string Name, string SqlType, string DataType);

    /// <summary>Schema discovered from the <c>DbDataReader</c> before any row is written.</summary>
    public sealed record QueryResultSchema(IReadOnlyList<QueryResultColumn> Columns);
}
