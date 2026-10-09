namespace FabricExplorerBackend.Commons.Models.Requests.Paginations
{
    public class PaginationWithCursorRequest
    {
        public string? ContinuationToken { get; set; }
        public int PageSize { get; set; } = 20;

        public void Validate()
        {
            PageSize = PageSize <= 0 || PageSize > 100 ? 20 : PageSize;
        }
    }
}
