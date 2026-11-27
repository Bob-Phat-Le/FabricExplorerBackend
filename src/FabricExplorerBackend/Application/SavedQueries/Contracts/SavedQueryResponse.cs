using FabricExplorerBackend.Commons.Paginations;

namespace FabricExplorerBackend.Application.SavedQueries.Contracts
{
    public sealed record SavedQueryResponse(
        Guid Id,
        Guid ExternalKey,
        string Name,
        string? Description,
        Guid ConnectionId,
        string? ConnectionName,
        SavedQueryTargetResponse Target,
        string Language,
        string Sql,
        int? MaxRows,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
