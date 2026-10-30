using FabricExplorerBackend.Application.QueryService.Contracts;
using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Application.QueryService
{
    internal static class QueryMapping
    {
        public static QueryResponse ToResponse(this QueryMetadata query)
            => new(query.Id, query.ConnectionId, query.Sql, query.Status, query.RowCount, query.Error,
                query.CreatedAt, query.CompletedAt, query.ExpiresAt);

        public static QueryMetadataStreamEvent ToStreamEvent(this QueryMetadata query)
            => new(query.Id, query.UserId, query.ConnectionId, query.Sql, query.Status,
                query.RowCount, query.Error, query.CompletedAt, query.ExpiresAt);
    }
}
