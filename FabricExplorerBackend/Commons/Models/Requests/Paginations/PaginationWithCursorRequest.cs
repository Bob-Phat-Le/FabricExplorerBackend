namespace FabricExplorerBackend.Commons.Models.Requests.Paginations
{
    public class PaginationWithCursorRequest
    {
        public string? ContinuationToken { get; set; }
        public int PageSize { get; set; } = 20;
    }
}
