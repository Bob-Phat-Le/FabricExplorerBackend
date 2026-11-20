namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    public record XsdRecordField
    (
        string Name,
        string XsdType,
        string RecommendedFabricType,
        bool Nullable,
        bool IsKeyCandidate,
        bool IsAttribute = false,
        bool IsNested = false,
        bool IsRepeating = false
    );
}
