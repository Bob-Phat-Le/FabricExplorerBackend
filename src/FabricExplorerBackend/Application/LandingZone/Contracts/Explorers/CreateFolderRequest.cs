namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record CreateFolderRequest(string? ParentPath, string? Name);
}
