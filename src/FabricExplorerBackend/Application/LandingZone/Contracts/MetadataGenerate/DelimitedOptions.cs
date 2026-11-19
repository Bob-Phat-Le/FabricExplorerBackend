namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    public record DelimitedOptions(
        string? FileExtension,
        bool FirstRowAsHeader,
        string? ColumnSeparator,
        string? RowSeparator,
        string? QuoteCharacter,
        string? EscapeCharacter,
        string? Encoding,
        string? NullValue
    );
}
