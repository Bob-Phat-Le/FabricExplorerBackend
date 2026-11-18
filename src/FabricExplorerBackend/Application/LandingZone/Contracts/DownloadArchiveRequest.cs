namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record DownloadArchiveRequest(IReadOnlyList<string>? Paths);
}
