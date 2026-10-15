namespace FabricExplorerBackend.Infrastructures.Options
{
    public class AuthenticationOptions
    {
        public const string SectionName = "Authentication";

        public TimeSpan TokenCacheSafetyMargin { get; init; } = TimeSpan.FromMinutes(5);
    }
}
