namespace FabricExplorerBackend.Infrastructures.Options
{
    public class QueryOptions
    {
        public const string SectionName = "Query";

        /// <summary>How long a result (and its metadata) is kept after the query finished.</summary>
        public TimeSpan ResultRetention { get; init; } = TimeSpan.FromHours(1);

        /// <summary>SQL command timeout of a single query. <see cref="TimeSpan.Zero"/> means no timeout.</summary>
        public TimeSpan CommandTimeout { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>How many queries are executed at the same time by the background workers.</summary>
        public int MaxConcurrentQueries { get; init; } = 4;

        /// <summary>How many queries can wait for a worker before new submissions are rejected (503).</summary>
        public int QueueCapacity { get; init; } = 100;

        /// <summary>Number of rows buffered in memory and written as one Parquet row group.</summary>
        public int RowGroupSize { get; init; } = 10_000;

        /// <summary>A Parquet part is finalized and a new one started once it reaches this size.</summary>
        public long MaxPartSizeBytes { get; init; } = 64L * 1024 * 1024;

        public int MaxSqlLength { get; init; } = 100_000;

        public int DefaultPageSize { get; init; } = 100;
        public int MaxPageSize { get; init; } = 1_000;

        public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromMinutes(5);
        public int CleanupBatchSize { get; init; } = 50;
    }
}
