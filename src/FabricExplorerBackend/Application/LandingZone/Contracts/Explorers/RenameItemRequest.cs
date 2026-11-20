namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record RenameItemRequest(string? Path, bool IsFolder, string? NewName);
}
