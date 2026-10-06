namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.Paginations
{
    public class PageInformationWithCursor
    {
        public int? TotalItems { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage => !string.IsNullOrEmpty(NextToken);
        public string? NextToken { get; set; }
    }
}
