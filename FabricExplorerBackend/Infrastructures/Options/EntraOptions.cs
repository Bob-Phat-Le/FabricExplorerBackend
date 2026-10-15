namespace FabricExplorerBackend.Infrastructures.Options
{
    public class EntraOptions
    {
        public const string SectionName = "Entra";

        public string Instance { get; init; } = "https://login.microsoftonline.com/";
        public string TenantId { get; init; } = "YOUR_TENANT_ID";
        public string ClientId { get; init; } = "YOUR_API_CLIENT_ID";
    }
}
