namespace FabricExplorerBackend.Application.SavedQueries.Contracts
{
    public sealed class SavedQueryTarget
    {
        public string? ItemType { get; set; }
        public Guid ItemId { get; set; }
        public string? ItemName { get; set; }
    }
}
