using System.Data.Common;

namespace FabricExplorerBackend.Application.QueryService
{
    /// <summary>Streams a <see cref="DbDataReader"/> into the result store, batch by batch.</summary>
    public interface IQueryResultWriter
    {
        /// <summary>
        /// Writes the first result set of <paramref name="reader"/> and its manifest.
        /// <paramref name="maxRows"/> null means "unlimited" (no artificial limit).
        /// On failure the caller is responsible for deleting the partial result from the store.
        /// </summary>
        Task<QueryResultMetadata> WriteAsync(
            Guid queryId,
            DbDataReader reader,
            long? maxRows,
            CancellationToken cancellationToken = default);
    }
}
