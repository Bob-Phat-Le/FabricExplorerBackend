namespace FabricExplorerBackend.Infrastructures.Options
{
    public class LandingZoneOptions
    {
        public const string SectionName = "LandingZone";

        public string OneLakeEndpoint { get; init; } = "https://onelake.dfs.fabric.microsoft.com";
        public string[] AllowedEndpointSuffixes { get; init; } = [".dfs.fabric.microsoft.com", ".dfs.core.windows.net"];
        public int MaxListItems { get; init; } = 5000;
        public int MaxSearchResults { get; init; } = 500;
        public int MaxSearchScanned { get; init; } = 50000;
        public int PreviewBytes { get; init; } = 64 * 1024;
        public int PreviewCsvRows { get; init; } = 5;
        public int MaxArchiveFiles { get; init; } = 200;
        public int MaxBatchItems { get; init; } = 200;
        public int HistoryRetention { get; init; } = 200;
    }
}
