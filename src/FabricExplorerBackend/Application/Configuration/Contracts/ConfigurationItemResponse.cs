namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed record ConfigurationItemResponse(
        string Type,
        Guid Id,
        Guid Key,
        string Name,
        string? Source,
        string? ParentName,
        DateTimeOffset? UpdatedAt,
        string Status,
        IReadOnlyList<string> Issues);
}
