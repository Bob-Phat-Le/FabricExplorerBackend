namespace FabricExplorerBackend.Application.LandingZone.Contracts.Explorers
{
    public sealed record LandingZoneOperationResponse(
        Guid Id,
        string Operation,
        string Item,
        string Path,
        // Completed | Failed
        string Status,
        string? Details,
        DateTimeOffset CreatedAt);
}
