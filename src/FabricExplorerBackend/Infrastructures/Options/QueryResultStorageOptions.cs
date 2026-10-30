namespace FabricExplorerBackend.Infrastructures.Options
{
    public class QueryResultStorageOptions
    {
        public const string SectionName = "QueryResultStorage";

        /// <summary>
        /// Root folder of the local result store. A relative path is resolved against the content root.
        /// Results of query 123 are stored in &lt;RootPath&gt;/123/.
        /// </summary>
        public string RootPath { get; init; } = "query-results";
    }
}
