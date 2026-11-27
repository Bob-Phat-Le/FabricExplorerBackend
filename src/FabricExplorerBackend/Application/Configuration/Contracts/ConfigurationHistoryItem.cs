namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed record ConfigurationHistoryItem(
        Guid Id,
        string Kind,
        string Status,
        string? Summary,
        int ItemCount,
        DateTimeOffset CreatedAt);
}
