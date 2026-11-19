namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record LandingZoneItemResult(string Path, string Name, bool Success, string? Error);
}
