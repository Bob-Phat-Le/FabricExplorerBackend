namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record LandingZoneConfigResponse(
        bool IsConfigured,
        string? StorageEndpoint,
        string? FileSystem,
        string? RootPath,
        // abfss://{file system}@{host}/{root}
        string? Uri,
        DateTimeOffset? UpdatedAt);
}
