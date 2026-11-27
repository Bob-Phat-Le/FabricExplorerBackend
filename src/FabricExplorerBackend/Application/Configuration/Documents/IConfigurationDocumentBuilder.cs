namespace FabricExplorerBackend.Application.Configuration.Documents
{
    public interface IConfigurationDocumentBuilder
    {
        int ConnectionCount { get; }
        int SavedQueryCount { get; }
        void AddConnection(ConnectionEntry entry);
        void AddSavedQuery(SavedQueryEntry entry);
        ConfigurationDocument Build(string scope);
    }
}
