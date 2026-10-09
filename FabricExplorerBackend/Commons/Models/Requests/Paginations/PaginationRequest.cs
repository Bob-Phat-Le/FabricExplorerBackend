namespace FabricExplorerBackend.Commons.Models.Requests.Paginations
{
    public class PaginationRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public void Validate()
        {
            Page = Page < 1 ? 1 : Page;
            PageSize = PageSize <= 0 || PageSize > 100 ? 20 : PageSize;
        }
    }
}
