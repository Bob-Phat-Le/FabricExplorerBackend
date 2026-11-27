namespace FabricExplorerBackend.Application.Configuration.Documents
{
    public sealed class SavedQueryEntry
    {
        public string? Key { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ConnectionKey { get; set; }
        public QueryTargetEntry? Target { get; set; }
        public string? Language { get; set; }
        public string? Sql { get; set; }
        public int? MaxRows { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
