namespace FabricExplorerBackend.Application.MirroredDatabase.Contracts
{
    public class LandingZoneListResponse
    {
        public List<LandingZoneItemResponse> Paths { get; set; } = [];
        public string? ContinuationToken { get; set; }
    }
}
