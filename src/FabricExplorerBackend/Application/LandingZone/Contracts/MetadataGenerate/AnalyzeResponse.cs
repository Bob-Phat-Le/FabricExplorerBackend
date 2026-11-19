namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    public record AnalyzeResponse
    (
        string TargetNamespace,
        IReadOnlyList<XsdRecord> Records
    );
}
