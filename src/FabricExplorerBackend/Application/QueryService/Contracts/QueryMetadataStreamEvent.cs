using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Application.QueryService.Contracts
{
    public sealed record QueryMetadataStreamEvent(
        Guid Id,
        Guid UserId,
        Guid ConnectionId,
        string Sql,
        QueryStatus Status,
        long RowCount = 0,
        string? Error = null,
        DateTimeOffset? CompletedAt = null,
        DateTimeOffset? ExpiresAt = null
    );
}
