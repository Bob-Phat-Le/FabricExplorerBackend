namespace FabricExplorerBackend.Configurations.Options
{
    public class AzureOptions
    {
        public const string SectionName = "Azure";

        public ScopeOptions Scopes { get; init; } = null!;
    }

    public class ScopeOptions
    {
        public const string SectionName = "Azure:Scopes";

        public string Fabric { get; init; } = "https://api.fabric.microsoft.com/.default";
        public string Storage { get; init; } = "https://storage.azure.com/.default";
        public string Sql { get; init; } = "https://database.windows.net/.default";
    }
}
