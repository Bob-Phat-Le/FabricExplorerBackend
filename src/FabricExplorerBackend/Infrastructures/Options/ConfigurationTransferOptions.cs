namespace FabricExplorerBackend.Infrastructures.Options
{
    /// <summary>Limits of configuration import / export.</summary>
    public class ConfigurationTransferOptions
    {
        public const string SectionName = "ConfigurationTransfer";

        /// <summary>Format versions this build can read.</summary>
        public int[] SupportedFormatVersions { get; init; } = [1];
        public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;
        public int MaxEntriesPerType { get; init; } = 1000;
        /// <summary>How many import / export records are kept per user.</summary>
        public int HistoryRetention { get; init; } = 200;
    }
}
