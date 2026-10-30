namespace FabricExplorerBackend.Application.QueryService
{
    /// <summary>Reads a stored result in pages; hides that a result may span several Parquet parts.</summary>
    public interface IQueryResultReader
    {
        /// <summary>Null when there is no stored result for the query.</summary>
        Task<QueryResultPage?> ReadPageAsync(Guid queryId, long offset, int limit, CancellationToken cancellationToken = default);
    }
}
