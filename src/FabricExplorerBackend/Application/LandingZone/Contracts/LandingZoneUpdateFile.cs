namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record LandingZoneUploadFile(string FileName, long Length, Func<Stream> OpenReadStream);
}
