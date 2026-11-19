namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    public record PreviewRequest(
        string FileFormat,
        IReadOnlyList<string> KeyColumns,
        IReadOnlyList<MetadataColumn> Columns,
        DelimitedOptions? Options
    );

}
