namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record TransferItemsRequest(IReadOnlyList<LandingZoneItemRef>? Items, string? DestinationPath);
}
