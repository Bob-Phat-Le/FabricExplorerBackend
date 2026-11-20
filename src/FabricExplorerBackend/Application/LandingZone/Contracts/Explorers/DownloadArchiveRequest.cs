namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record DownloadArchiveRequest(IReadOnlyList<string>? Paths);
}
