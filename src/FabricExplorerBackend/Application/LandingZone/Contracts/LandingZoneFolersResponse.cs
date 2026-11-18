namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record LandingZoneFoldersResponse(
        string Path,
        IReadOnlyList<LandingZoneFolderResponse> Folders,
        bool Truncated);
}
