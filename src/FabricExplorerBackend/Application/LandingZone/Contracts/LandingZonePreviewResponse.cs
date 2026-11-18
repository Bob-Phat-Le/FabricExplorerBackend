namespace FabricExplorerBackend.Application.LandingZone.Contracts
{
    public sealed record LandingZonePreviewResponse(
        string Kind,
        IReadOnlyList<string>? Headers,
        IReadOnlyList<IReadOnlyList<string>>? Rows,
        string? Text,
        // True when only the beginning of the file was read or not every row is shown.
        bool Truncated,
        string? Delimiter);
}
