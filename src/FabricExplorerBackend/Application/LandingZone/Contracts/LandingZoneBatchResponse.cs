namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record LandingZoneBatchResponse(
        IReadOnlyList<LandingZoneItemResult> Results,
        int Succeeded,
        int Failed);
}
