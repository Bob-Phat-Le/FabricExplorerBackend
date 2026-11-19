namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record TransferItemsRequest(IReadOnlyList<LandingZoneItemRef>? Items, string? DestinationPath);
}
