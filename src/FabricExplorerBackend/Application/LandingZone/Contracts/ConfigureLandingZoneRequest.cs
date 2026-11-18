namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record ConfigureLandingZoneRequest(Guid? MirroredDatabaseId, string? StorageUri);
}
