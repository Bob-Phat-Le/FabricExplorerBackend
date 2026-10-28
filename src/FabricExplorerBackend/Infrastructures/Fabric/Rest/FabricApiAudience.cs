namespace FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient
{
    /// <summary>
    /// Selects which Entra ID audience (scope) the access token is issued for.
    /// Fabric REST API and OneLake Table API use different audiences.
    /// </summary>
    public enum FabricApiAudience
    {
        /// <summary>https://api.fabric.microsoft.com/.default</summary>
        Fabric = 0,

        /// <summary>https://storage.azure.com/.default (OneLake Table API, additionalMetadata...)</summary>
        Storage = 1
    }
}
