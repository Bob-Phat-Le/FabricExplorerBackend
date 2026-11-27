namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed record ConfigurationCapabilitiesResponse(
        string Format,
        IReadOnlyList<int> SupportedFormatVersions,
        long MaxFileSizeBytes,
        int MaxEntriesPerType,
        IReadOnlyList<ConfigurationTypeInfo> Types,
        IReadOnlyList<string> ImportModes);
}
