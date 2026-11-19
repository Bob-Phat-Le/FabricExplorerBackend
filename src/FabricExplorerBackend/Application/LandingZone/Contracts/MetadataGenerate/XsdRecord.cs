namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    public record XsdRecord
    (
        string Name,
        IReadOnlyList<XsdRecordField> Fields
    );
}
