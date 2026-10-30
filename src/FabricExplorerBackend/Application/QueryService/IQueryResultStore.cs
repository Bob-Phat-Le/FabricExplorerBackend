namespace FabricExplorerBackend.Application.QueryService
{
    /// <summary>
    /// Bridge between the query pipeline and the physical storage (local disk today, object storage later).
    /// The physical location is always derived from the query id, never stored in the Query entity.
    /// </summary>
    public interface IQueryResultStore
    {
        /// <summary>
        /// Starts a new result part. The returned sink exposes a seekable writable stream (Parquet needs a
        /// footer, so remote implementations should buffer to a local temp file and upload in
        /// <see cref="IQueryResultPartWriter.CommitAsync"/>).
        /// </summary>
        Task<IQueryResultPartWriter> CreatePartWriterAsync(Guid queryId, int partNumber, CancellationToken cancellationToken = default);

        /// <summary>Opens a committed part for reading (seekable).</summary>
        Task<Stream> OpenPartReaderAsync(Guid queryId, string partName, CancellationToken cancellationToken = default);

        /// <summary>Writes the manifest. It is written last: a result without manifest is not readable.</summary>
        Task WriteMetadataAsync(Guid queryId, QueryResultMetadata metadata, CancellationToken cancellationToken = default);

        /// <summary>Null when the result does not exist (never written, failed, or already deleted).</summary>
        Task<QueryResultMetadata?> ReadMetadataAsync(Guid queryId, CancellationToken cancellationToken = default);

        /// <summary>Removes everything stored for the query (idempotent).</summary>
        Task DeleteAsync(Guid queryId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Write side of one result part. Disposing without <see cref="CommitAsync"/> discards the part.
    /// </summary>
    public interface IQueryResultPartWriter : IAsyncDisposable
    {
        string PartName { get; }

        /// <summary>Seekable, writable stream the Parquet writer writes into.</summary>
        Stream Stream { get; }

        /// <summary>Call after the Parquet footer was written. Makes the part visible/durable and returns its size.</summary>
        Task<long> CommitAsync(CancellationToken cancellationToken = default);
    }
}
