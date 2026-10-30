using FabricExplorerBackend.Application.QueryService;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;
using Parquet;
using Parquet.Schema;
using System.Data.Common;

namespace FabricExplorerBackend.Infrastructures.QueryResults
{
    /// <summary>
    /// Streams the reader into Parquet: rows are buffered per row group (<see cref="QueryOptions.RowGroupSize"/>),
    /// written, then released - the result is never held in memory. A part is rotated once its file reaches
    /// <see cref="QueryOptions.MaxPartSizeBytes"/>.
    /// </summary>
    public sealed class ParquetResultWriter(
        IQueryResultStore store,
        IOptions<QueryOptions> options,
        TimeProvider clock,
        ILogger<ParquetResultWriter> logger) : IQueryResultWriter
    {
        public async Task<QueryResultMetadata> WriteAsync(
            Guid queryId,
            DbDataReader reader,
            long? maxRows,
            CancellationToken cancellationToken = default)
        {
            var opt = options.Value;
            var createdAt = clock.GetUtcNow();
            var rowGroupSize = Math.Max(1, opt.RowGroupSize);

            var columns = CreateColumns(reader, rowGroupSize);
            if (columns.Count == 0)
            {
                // Statement without a result set (DDL/DML): Parquet cannot store an empty schema, so keep only the manifest.
                var empty = new QueryResultMetadata(0, 0, 0, [], false, createdAt, clock.GetUtcNow(), new QueryResultSchema([]));
                await store.WriteMetadataAsync(queryId, empty, cancellationToken);
                return empty;
            }

            var schema = new ParquetSchema(columns.Select(c => (Field)c.Field).ToArray());
            var parts = new List<QueryResultPart>();
            long totalRows = 0;
            long totalBytes = 0;
            var truncated = false;
            OpenPart? part = null;

            try
            {
                var limitReached = false;
                while (!limitReached)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = 0;
                    var exhausted = false;
                    while (batch < rowGroupSize)
                    {
                        if (maxRows is { } max && totalRows + batch >= max)
                        {
                            limitReached = true;
                            break;
                        }

                        if (!await reader.ReadAsync(cancellationToken))
                        {
                            exhausted = true;
                            break;
                        }

                        for (var i = 0; i < columns.Count; i++)
                            columns[i].Append(reader, i);
                        batch++;
                    }

                    if (batch > 0)
                    {
                        part ??= await OpenPartAsync(queryId, parts.Count + 1, schema, totalRows, cancellationToken);

                        using (var rowGroup = part.Writer.CreateRowGroup())
                        {
                            foreach (var column in columns)
                                await rowGroup.WriteColumnAsync(column.Flush(), cancellationToken);
                        }

                        part.Rows += batch;
                        part.RowGroups++;
                        totalRows += batch;

                        if (part.Sink.Stream.Position >= opt.MaxPartSizeBytes)
                        {
                            totalBytes += await ClosePartAsync(part, parts, cancellationToken);
                            part = null;
                        }
                    }

                    if (exhausted)
                        break;
                }

                // The limit was hit: is there anything left? (tells the UI the result was cut)
                if (limitReached)
                    truncated = await reader.ReadAsync(cancellationToken);

                if (part is not null)
                {
                    totalBytes += await ClosePartAsync(part, parts, cancellationToken);
                    part = null;
                }
            }
            finally
            {
                // Only reached with an open part on failure/cancellation: discard it.
                if (part is not null)
                    await part.AbortAsync();
            }

            var metadata = new QueryResultMetadata(
                totalRows,
                totalBytes,
                parts.Count,
                parts,
                truncated,
                createdAt,
                clock.GetUtcNow(),
                new QueryResultSchema(columns.Select(c => c.Descriptor).ToList()));

            // The manifest goes last: only a fully written result becomes readable.
            await store.WriteMetadataAsync(queryId, metadata, cancellationToken);

            logger.LogInformation(
                "Query {QueryId}: stored {Rows} rows in {Parts} part(s), {Bytes} bytes{Truncated}",
                queryId, totalRows, parts.Count, totalBytes, truncated ? " (truncated)" : string.Empty);

            return metadata;
        }

        private async Task<OpenPart> OpenPartAsync(
            Guid queryId, int partNumber, ParquetSchema schema, long startRow, CancellationToken cancellationToken)
        {
            var sink = await store.CreatePartWriterAsync(queryId, partNumber, cancellationToken);
            try
            {
                var writer = await ParquetWriter.CreateAsync(schema, sink.Stream, cancellationToken: cancellationToken);
                return new OpenPart(sink, writer, startRow);
            }
            catch
            {
                await sink.DisposeAsync();
                throw;
            }
        }

        private static async Task<long> ClosePartAsync(OpenPart part, List<QueryResultPart> parts, CancellationToken cancellationToken)
        {
            // Disposing the Parquet writer writes the footer; only then can the part be committed.
            part.Writer.Dispose();
            var size = await part.Sink.CommitAsync(cancellationToken);
            part.Committed = true;
            await part.Sink.DisposeAsync();

            parts.Add(new QueryResultPart(part.Sink.PartName, part.StartRow, part.Rows, size, part.RowGroups));
            return size;
        }

        private static List<ParquetColumn> CreateColumns(DbDataReader reader, int capacity)
        {
            var columns = new List<ParquetColumn>();
            if (reader.FieldCount == 0)
                return columns;

            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var schema = reader.GetColumnSchema();

            for (var i = 0; i < schema.Count; i++)
                columns.Add(ParquetColumnFactory.Create(schema[i], UniqueName(schema[i].ColumnName, i, used), capacity));

            return columns;
        }

        // SQL allows empty and duplicate column names (SELECT COUNT(*), a.Id, b.Id), Parquet does not.
        // '.' is replaced because it is the separator of Parquet column paths.
        private static string UniqueName(string? raw, int ordinal, HashSet<string> used)
        {
            var name = string.IsNullOrWhiteSpace(raw) ? $"Column{ordinal + 1}" : raw.Replace('.', '_');

            var candidate = name;
            for (var n = 1; !used.Add(candidate); n++)
                candidate = $"{name}_{n}";

            return candidate;
        }

        private sealed class OpenPart(IQueryResultPartWriter sink, ParquetWriter writer, long startRow)
        {
            public IQueryResultPartWriter Sink { get; } = sink;
            public ParquetWriter Writer { get; } = writer;
            public long StartRow { get; } = startRow;
            public long Rows { get; set; }
            public int RowGroups { get; set; }
            public bool Committed { get; set; }

            public async Task AbortAsync()
            {
                if (Committed)
                    return;

                try { Writer.Dispose(); } catch { /* the part is discarded anyway */ }
                await Sink.DisposeAsync(); // not committed => the store discards it
            }
        }
    }
}
