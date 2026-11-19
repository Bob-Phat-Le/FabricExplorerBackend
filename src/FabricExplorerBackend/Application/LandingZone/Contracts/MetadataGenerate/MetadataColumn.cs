namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    public record MetadataColumn(
        string Name,
        string FabricType,
        bool Included,
        bool Nullable
    );
}
