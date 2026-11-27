namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed record PreviewResponse(
        int? FormatVersion,
        bool IntegrityValid,
        IReadOnlyList<FileIssue> FileErrors,
        IReadOnlyList<PreviewEntry> Entries,
        PreviewSummary Summary);
}
