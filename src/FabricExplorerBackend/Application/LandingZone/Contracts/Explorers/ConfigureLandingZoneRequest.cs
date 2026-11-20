namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record ConfigureLandingZoneRequest(Guid? MirroredDatabaseId, string? StorageUri);
}
