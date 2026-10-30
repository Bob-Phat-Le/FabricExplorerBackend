using FabricExplorerBackend.Application.QueryService;
using Parquet;

namespace FabricExplorerBackend.Infrastructures.QueryResults
{
    /// <summary>
    /// Reads a window of rows. The manifest tells which parts overlap the window, the Parquet footers tell
    /// which row groups do, so only those row groups are read - never the whole result.
    /// </summary>
    public sealed class ParquetResultReader(IQueryResultStore store) : IQueryResultReader
    {
        public async Task<QueryResultPage?> ReadPageAsync(
            Guid queryId, long offset, int limit, CancellationToken cancellationToken = default)
        {
            var metadata = await store.ReadMetadataAsync(queryId, cancellationToken);
            if (metadata is null)
                return null;

            var columns = metadata.Schema.Columns;
            var rows = new List<object?[]>();

            offset = Math.Max(0, offset);
            var end = Math.Min(offset + Math.Max(0, limit), metadata.RowCount);

            if (columns.Count > 0 && offset < end)
            {
                foreach (var part in metadata.Parts)
                {
                    if (part.StartRow + part.RowCount <= offset)
                        continue;
                    if (part.StartRow >= end)
                        break;

                    await ReadPartAsync(queryId, part, columns, offset, end, rows, cancellationToken);
                }
            }

            return new QueryResultPage(metadata.RowCount, metadata.IsTruncated, columns, rows);
        }

        private async Task ReadPartAsync(
            Guid queryId,
            QueryResultPart part,
            IReadOnlyList<QueryResultColumn> columns,
            long offset,
            long end,
            List<object?[]> rows,
            CancellationToken cancellationToken)
        {
            await using var stream = await store.OpenPartReaderAsync(queryId, part.Name, cancellationToken);
            using var parquet = await ParquetReader.CreateAsync(stream, cancellationToken: cancellationToken);

            var fields = parquet.Schema.GetDataFields();
            var groupStart = part.StartRow;

            for (var g = 0; g < parquet.RowGroupCount; g++)
            {
                var group = parquet.OpenRowGroupReader(g);
                var groupRows = (long)group.RowCount;
                var groupEnd = groupStart + groupRows;

                if (groupEnd <= offset)
                {
                    groupStart = groupEnd;
                    continue;
                }

                if (groupStart >= end)
                    break;

                var data = new Array[fields.Length];
                for (var f = 0; f < fields.Length; f++)
                    data[f] = (await group.ReadColumnAsync(fields[f], cancellationToken)).Data;

                var from = (int)Math.Max(0, offset - groupStart);
                var to = (int)Math.Min(groupRows, end - groupStart);

                for (var r = from; r < to; r++)
                {
                    var row = new object?[fields.Length];
                    for (var f = 0; f < fields.Length; f++)
                        row[f] = ToClientValue(data[f].GetValue(r), columns[f].DataType);
                    rows.Add(row);
                }

                groupStart = groupEnd;
            }
        }

        private static object? ToClientValue(object? value, string dataType) => value switch
        {
            DateTime dt when dataType == "date" => DateOnly.FromDateTime(dt),
            // SQL datetime has no time zone: do not let the JSON serializer append a 'Z'.
            DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Unspecified),
            _ => value
        };
    }
}
