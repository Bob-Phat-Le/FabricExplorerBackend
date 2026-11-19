namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record LandingZoneBatchResponse(
        IReadOnlyList<LandingZoneItemResult> Results,
        int Succeeded,
        int Failed);
}
