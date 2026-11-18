namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record LandingZoneFileDetailsResponse(
        string Name,
        string Path,
        string? Extension,
        long SizeBytes,
        string ContentType,
        DateTimeOffset? CreatedAt,
        DateTimeOffset? LastModified,
        string StorageUri);
}
