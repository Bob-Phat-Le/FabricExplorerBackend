namespace FabricExplorerBackend.Infrastructures.BackgroundJobs
{
    public class OperationPollingOptions
    {
        public const string Section = "Fabric:OperationPolling";

        public bool Enabled { get; set; } = true;
        public int BatchSize { get; set; } = 20;
        public int MaxConsecutivePollFailures { get; set; } = 10;
        public int TickIntervalSeconds { get; set; } = 5;
        public int LeaseDurationMinutes { get; set; } = 2;
        public int MaxOperationAgeHours { get; set; } = 6;
        public int BaseBackoffSeconds { get; set; } = 10;
        public int MaxBackoffMinutes { get; set; } = 2;

        public TimeSpan TickInterval => TimeSpan.FromSeconds(TickIntervalSeconds);
        public TimeSpan LeaseDuration => TimeSpan.FromMinutes(LeaseDurationMinutes);
        public TimeSpan MaxOperationAge => TimeSpan.FromHours(MaxOperationAgeHours);
        public TimeSpan BaseBackoff => TimeSpan.FromSeconds(BaseBackoffSeconds);
        public TimeSpan MaxBackoff => TimeSpan.FromMinutes(MaxBackoffMinutes);
    }
}
