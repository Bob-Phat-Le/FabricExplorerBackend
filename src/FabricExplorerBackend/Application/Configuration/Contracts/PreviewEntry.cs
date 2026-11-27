namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed record PreviewEntry(
        string Type,
        string? Key,
        string? Name,
        MatchInfo Match,
        ValidationResult Validation,
        IReadOnlyList<string> Flags);
}
