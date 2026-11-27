namespace FabricExplorerBackend.Application.Configuration.Documents
{
    public static class ConfigurationFormat
    {
        public const string Name = "fabric-explorer.configuration";
        public const int CurrentVersion = 1;
        public const string ScopeAll = "all";
        public const string ScopeSelected = "selected";
        public const string IntegrityAlgorithm = "sha256";
        public const string SecretExcluded = "excluded";

        /// <summary>An export without a scope exports everything.</summary>
        public static string NormalizeScope(string? scope) => (scope ?? ScopeAll).Trim().ToLowerInvariant();
    }
}
