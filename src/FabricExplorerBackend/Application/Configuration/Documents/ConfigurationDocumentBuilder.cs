namespace FabricExplorerBackend.Application.Configuration.Documents
{
    /// <summary>Builder of the exported document. Handlers add entries; <see cref="Build"/> seals it.</summary>
    public sealed class ConfigurationDocumentBuilder(IIntegrityHasher hasher, TimeProvider clock) : IConfigurationDocumentBuilder
    {
        public const string ConnectionsCollection = "connections";
        public const string SavedQueriesCollection = "savedQueries";

        private readonly List<ConnectionEntry> connections = [];
        private readonly List<SavedQueryEntry> savedQueries = [];

        public int ConnectionCount => connections.Count;
        public int SavedQueryCount => savedQueries.Count;

        public void AddConnection(ConnectionEntry entry) => connections.Add(entry);

        public void AddSavedQuery(SavedQueryEntry entry) => savedQueries.Add(entry);

        public ConfigurationDocument Build(string scope)
        {
            var document = new ConfigurationDocument
            {
                Format = ConfigurationFormat.Name,
                FormatVersion = ConfigurationFormat.CurrentVersion,
                ExportedAt = clock.GetUtcNow(),
                App = new ConfigurationAppInfo
                {
                    Name = "Fabric Explorer",
                    Version = typeof(ConfigurationDocumentBuilder).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"
                },
                Scope = scope,
                Contents = new Dictionary<string, int>
                {
                    [ConnectionsCollection] = connections.Count,
                    [SavedQueriesCollection] = savedQueries.Count
                },
                Connections = connections,
                SavedQueries = savedQueries,
                Preferences = null
            };

            document.Integrity = hasher.Compute(document);
            return document;
        }
    }
}
