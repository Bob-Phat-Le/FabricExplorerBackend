namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record LandingZoneListResponse(
        string Path,
        IReadOnlyList<LandingZoneItemResponse> Items,
        // True when the folder/search has more entries than the configured limit.
        bool Truncated);
}
