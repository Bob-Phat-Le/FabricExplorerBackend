namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record LandingZoneFoldersResponse(
        string Path,
        IReadOnlyList<LandingZoneFolderResponse> Folders,
        bool Truncated);
}
