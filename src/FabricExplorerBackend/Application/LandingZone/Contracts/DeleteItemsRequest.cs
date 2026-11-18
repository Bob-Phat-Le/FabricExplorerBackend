namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record DeleteItemsRequest(IReadOnlyList<LandingZoneItemRef>? Items);
}
