namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    /// <summary>The requested names that already exist in the folder (a folder with the same name counts too).</summary>
    public sealed record CheckFilesExistResponse(IReadOnlyList<string> Existing);
}
