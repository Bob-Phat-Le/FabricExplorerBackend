namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record DeleteItemsRequest(IReadOnlyList<LandingZoneItemRef>? Items);
}
