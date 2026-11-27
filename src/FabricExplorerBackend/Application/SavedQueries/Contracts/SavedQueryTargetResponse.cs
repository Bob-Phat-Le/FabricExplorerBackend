namespace FabricExplorerBackend.Application.SavedQueries.Contracts
{
    public sealed record SavedQueryTargetResponse(string ItemType, Guid ItemId, string ItemName);
}
