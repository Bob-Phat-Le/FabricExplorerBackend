namespace FabricExplorerBackend.Infrastructures.Fabric.Dtos
{
    public sealed class TableMirroringStatusDto
    {
        public string? SourceSchemaName { get; set; }
        public string? SourceTableName { get; set; }
        public string? Status { get; set; }
        public TableMirroringMetricsDto? Metrics { get; set; }
    }

    public sealed class TableMirroringMetricsDto
    {
        public long? ProcessedBytes { get; set; }
        public long? ProcessedRows { get; set; }
        public DateTimeOffset? LastSyncDateTime { get; set; }
        public int? LastSyncLatencyInSeconds { get; set; }
    }
}
