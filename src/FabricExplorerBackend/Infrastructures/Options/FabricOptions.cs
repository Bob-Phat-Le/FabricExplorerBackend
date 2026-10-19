namespace FabricExplorerBackend.Infrastructures.Options
{
    public class FabricOptions
    {
        public const string SectionName = "Fabric";

        public FabricApiOptions Api { get; init; } = null!;
        public SqlSchemas SqlSchemas { get; init; } = null!;
    }

    public class FabricApiOptions
    {
        public const string SectionName = "Fabric:Api";

        public string BaseUrl { get; init; } = "https://api.fabric.microsoft.com/v1/";
    }

    public class SqlSchemas
    {
        public const string SectionName = "Fabric:SqlSchemas";

        public string Default { get; init; } = "dbo";
    }
}
