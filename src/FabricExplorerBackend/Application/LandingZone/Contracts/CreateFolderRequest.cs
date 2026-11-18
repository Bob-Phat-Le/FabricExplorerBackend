namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record CreateFolderRequest(string? ParentPath, string? Name);
}
