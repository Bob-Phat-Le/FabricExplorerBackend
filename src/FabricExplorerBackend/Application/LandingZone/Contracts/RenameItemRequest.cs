namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record RenameItemRequest(string? Path, bool IsFolder, string? NewName);
}
