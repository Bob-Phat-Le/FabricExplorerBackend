namespace FabricExplorerBackend.Application.SavedQueries.Contracts
{
    /// <summary>Only the fields that are sent are changed. Use <see cref="UnlimitedRows"/> to clear maxRows.</summary>
    public sealed class UpdateSavedQueryRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public SavedQueryTarget? Target { get; set; }
        public string? Language { get; set; }
        public string? Sql { get; set; }
        public int? MaxRows { get; set; }
        public bool UnlimitedRows { get; set; }
    }
}
