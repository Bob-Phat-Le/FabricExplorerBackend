namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    /// <param name="Options">Values of the format options (see <see cref="FormatOption.Key"/>). Missing keys use the default.</param>
    public record PreviewRequest(
        string FileFormat,
        IReadOnlyList<string> KeyColumns,
        IReadOnlyList<MetadataColumn> Columns,
        IReadOnlyDictionary<string, string?>? Options = null
    );
}
