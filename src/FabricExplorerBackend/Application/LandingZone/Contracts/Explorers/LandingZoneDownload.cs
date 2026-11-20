namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record LandingZoneDownload(Stream Content, string FileName, string ContentType);
}
