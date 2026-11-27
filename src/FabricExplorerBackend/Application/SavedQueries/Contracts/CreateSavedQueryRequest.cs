namespace FabricExplorerBackend.Application.SavedQueries.Contracts
{
    public sealed class CreateSavedQueryRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Guid ConnectionId { get; set; }
        public SavedQueryTarget? Target { get; set; }
        public string? Language { get; set; }
        public string? Sql { get; set; }
        /// <summary>null = unlimited.</summary>
        public int? MaxRows { get; set; }
    }
}
