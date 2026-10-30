using FabricExplorerBackend.Domain.Enums;
using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Application.QueryService.Contracts
{
    public sealed record QueryResponse(
        Guid Id,
        Guid ConnectionId,
        string Sql,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] QueryStatus Status,
        long RowCount,
        string? Error,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset ExpiresAt);
}
