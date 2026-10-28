namespace FabricExplorerBackend.Infrastructures.Options
{
    public class CatalogOptions
    {
        public const string SectionName = "Catalog";

        /// <summary>Cache lifetime of the schema/table tree of a lakehouse.</summary>
        public TimeSpan TreeCache { get; init; } = TimeSpan.FromMinutes(2);

        /// <summary>Cache lifetime of the column details of a single table.</summary>
        public TimeSpan TableDetailCache { get; init; } = TimeSpan.FromMinutes(2);

        /// <summary>How many schemas are listed concurrently when building the tree.</summary>
        public int SchemaParallelism { get; init; } = 4;
    }
}
