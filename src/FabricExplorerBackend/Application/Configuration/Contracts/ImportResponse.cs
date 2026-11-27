namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed record ImportResponse(
        int Created,
        int Updated,
        int Skipped,
        int Failed,
        IReadOnlyList<ImportResultItem> Results);
}
