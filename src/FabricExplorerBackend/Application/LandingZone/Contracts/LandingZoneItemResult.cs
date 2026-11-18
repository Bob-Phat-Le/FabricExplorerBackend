namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record LandingZoneItemResult(string Path, string Name, bool Success, string? Error);
}
