namespace FabricExplorerBackend.Commons.Models.Responses.Paginations
{
    public class PageInformationWithCursor
    {
        //public int TotalItems { get; set; }
        public int ItemsCount { get; set; }
        public int PageSize { get; set; } = 20;
        public bool HasNextPage => !string.IsNullOrEmpty(NextToken);
        public string? NextToken { get; set; }
    }
}
