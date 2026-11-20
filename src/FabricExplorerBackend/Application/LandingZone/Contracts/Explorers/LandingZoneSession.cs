using FabricExplorerBackend.Infrastructures.LandingZone;

namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record Session(
        Domain.Entities.Connection Connection,
        LandingZoneLocation Location,
        ILandingZoneFileSystem FileSystem);
}
