namespace FabricExplorerBackend.Infrastructures.Options
{
    public class EventStreamingOptions
    {
        public const string SectionName = "EventStreaming";
        public TimeSpan CompletedRetention { get; init; } = TimeSpan.FromMinutes(2);
        public TimeSpan MaxLifetime { get; init; } = TimeSpan.FromMinutes(5);
        public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(15);
        public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromSeconds(30);
        public int MaxEventsPerStream { get; init; } = 2000;
        public int MaxActiveStreams { get; init; } = 1000;
    }
}
