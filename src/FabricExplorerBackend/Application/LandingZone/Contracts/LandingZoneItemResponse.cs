namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record LandingZoneItemResponse(
        string Name,
        // Path relative to the Landing Zone root.
        string Path,
        bool IsFolder,
        // Lower case, without dot. Null for folders and files without extension.
        string? Extension,
        long? SizeBytes,
        DateTimeOffset? LastModified,
        string? Owner);
}
