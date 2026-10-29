namespace FabricExplorerBackend.Configurations.Options
{
    public class ConnectionStringsOptions
    {
        public const string SectionName = "ConnectionStrings";

        public required string FabricExplorerDatabase { get; init; }
        public required string RedisCache { get; init; }
    }
}
