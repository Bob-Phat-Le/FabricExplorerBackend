namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    public record AnalyzeRequest(IFormFile File, string? FileFormat = null);
}
