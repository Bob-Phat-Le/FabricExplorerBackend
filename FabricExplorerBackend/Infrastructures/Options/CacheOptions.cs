namespace FabricExplorerBackend.Infrastructures.Options
{
    public class CacheOptions
    {
        public const string SectionName = "Cache";

        public string KeyPrefix { get; init; } = "FabricExplorer";
        public TimeSpan WorkspaceList { get; init; } = TimeSpan.FromMinutes(5);
        public TimeSpan ItemList { get; init; } = TimeSpan.FromMinutes(1);
        public TimeSpan TableStats { get; init; } = TimeSpan.FromMinutes(1);
        public TimeSpan MirroringDefinition { get; init; } = TimeSpan.FromHours(1);
        public TimeSpan TableStale { get; init; } = TimeSpan.FromDays(30);
        public TimeSpan SharedFetchTimeout { get; init; } = TimeSpan.FromSeconds(60);
        public TimeSpan WarehouseProbeTimeout { get; init; } = TimeSpan.FromSeconds(10);
    }
}
