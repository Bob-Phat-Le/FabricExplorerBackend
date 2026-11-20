namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    public record MetadataFormatInfo(
        string Name,
        string Title,
        string Description,
        bool RequiresColumnTypes,
        IReadOnlyList<FormatOption> Options);

    /// <summary>Everything the UI needs to build the wizard without hard-coding it.</summary>
    public record MetadataCapabilities(
        IReadOnlyList<MetadataFormatInfo> Formats,
        IReadOnlyList<string> FabricTypes,
        string MetadataFileName,
        string SchemaFolderSuffix);
}
