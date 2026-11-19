namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    public record PublishRequest(
        string ParentPath,
        string SchemaName,
        string TableName,
        bool Overwrite,
        PreviewRequest Metadata
    );
}
