using FabricExplorerBackend.Commons.Paginations;

namespace FabricExplorerBackend.Application.SavedQueries.Contracts
{
    /// <summary>GET /saved-queries. Page and PageSize come from <see cref="PaginationRequest"/>.</summary>
    public class ListSavedQueriesRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public Guid? ConnectionId { get; set; }
        public string? ItemType { get; set; }
    }
}
