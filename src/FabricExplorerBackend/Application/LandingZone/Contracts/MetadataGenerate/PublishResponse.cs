namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    public record PublishResponse(
        string FilePath,
        bool Overwritten
    );
}
