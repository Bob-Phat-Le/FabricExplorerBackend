namespace FabricExplorerBackend.Infrastructures.Options
{
    public class OperationPollingOptions
    {
        public const string SectionName = "OperationPolling";

        public bool Enabled { get; init; } = true;
        public TimeSpan TickInterval { get; init; } = TimeSpan.FromSeconds(5);
        public int BatchSize { get; init; } = 20;
        public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromMinutes(2);
        public TimeSpan MaxOperationAge { get; init; } = TimeSpan.FromHours(6);
        public int MaxConsecutivePollFailures = 5;
        public TimeSpan BaseBackoff { get; init; } = TimeSpan.FromSeconds(10);
        public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromMinutes(2);
        public int TableStatsMaxParallelism { get; init; } = 4;
    }
}
