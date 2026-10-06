namespace FabricExplorerBackend.Commons
{
    public class PaginationWithCursorRequest
    {
        public string? ContinuationToken { get; set; }
        public int PageSize { get; set; } = 20;
    }
}
