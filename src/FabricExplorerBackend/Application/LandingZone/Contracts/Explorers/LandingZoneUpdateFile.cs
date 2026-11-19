namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record LandingZoneUploadFile(string FileName, long Length, Func<Stream> OpenReadStream);
}
