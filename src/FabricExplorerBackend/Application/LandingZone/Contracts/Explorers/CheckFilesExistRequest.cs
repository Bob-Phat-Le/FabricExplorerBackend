namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    /// <summary>File names that are about to be uploaded into <see cref="Path"/> (relative to the Landing Zone root).</summary>
    public sealed record CheckFilesExistRequest(string? Path, IReadOnlyList<string>? Names);
}
