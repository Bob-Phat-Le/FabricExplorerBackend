namespace FabricExplorerBackend.Application.MirroredDatabase.Contracts
{
    public static class MirroredDatabaseStreamEvents
    {
        public const string MirroringStatus = "mirroring-status";
        public const string SourceInfo = "source-info";
        public const string MirroringMetrics = "mirroring-metrics";
    }

    public sealed record MirroredDatabaseStatusStreamEvent(MirroringStatus MirroringStatus, MirroredDatabaseOnlineStatus Status);
    public sealed record MirroredDatabaseSourceInfoStreamEvent(string? SourceType, string? SourceName);
    public sealed record MirroringMetricsStreamEvent(DateTimeOffset? LastSynchronization, long RecordSynchronized, int? CurrentLatency);
}
