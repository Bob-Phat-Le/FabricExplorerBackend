namespace FabricExplorerBackend.Commons
{
    public class PaginationRequest
    {
        public int? Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? ContinuationToken { get; set; }

        public bool IsValid()
        {
            return (Page.HasValue && ContinuationToken == null) || (Page == null && ContinuationToken != null);
        }
    }
}
