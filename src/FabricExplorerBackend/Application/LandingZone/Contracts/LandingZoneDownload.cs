namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record LandingZoneDownload(Stream Content, string FileName, string ContentType);
}
