namespace FabricExplorerBackend.Application.MirroredDatabase.Contracts
{
    public class LandingZoneRequestQuery
    {
        public string Resource { get; set; } = "filesystem";
        public bool Recursive { get; set; } = false;
    }
}
