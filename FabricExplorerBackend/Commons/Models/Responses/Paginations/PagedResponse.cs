namespace FabricExplorerBackend.Commons.Models.Responses.Paginations
{
    public class PagedResponse<T>
    {
        public T Items { get; init; }
        public PageInformation? PageInformation { get; set; } = null!;
        //public string? ContinuationToken { get; init; }

        public PagedResponse() { }
        public PagedResponse(T items, PageInformation? pageInformation)
        {
            Items = items;
            PageInformation = pageInformation;
        }
    }
}
