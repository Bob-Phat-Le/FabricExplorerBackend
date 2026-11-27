using FabricExplorerBackend.Commons.Paginations;

namespace FabricExplorerBackend.Application.SavedQueries.Contracts
{
    public sealed class SavedQueryTarget
    {
        public string? ItemType { get; set; }
        public Guid ItemId { get; set; }
        public string? ItemName { get; set; }
    }

    public sealed class CreateSavedQueryRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Guid ConnectionId { get; set; }
        public SavedQueryTarget? Target { get; set; }
        public string? Language { get; set; }
        public string? Sql { get; set; }
        /// <summary>null = unlimited.</summary>
        public int? MaxRows { get; set; }
    }

    /// <summary>Only the fields that are sent are changed. Use <see cref="UnlimitedRows"/> to clear maxRows.</summary>
    public sealed class UpdateSavedQueryRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public SavedQueryTarget? Target { get; set; }
        public string? Language { get; set; }
        public string? Sql { get; set; }
        public int? MaxRows { get; set; }
        public bool UnlimitedRows { get; set; }
    }

    /// <summary>GET /saved-queries. Page and PageSize come from <see cref="PaginationRequest"/>.</summary>
    public class ListSavedQueriesRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public Guid? ConnectionId { get; set; }
        public string? ItemType { get; set; }
    }

    public sealed record SavedQueryTargetResponse(string ItemType, Guid ItemId, string ItemName);

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
