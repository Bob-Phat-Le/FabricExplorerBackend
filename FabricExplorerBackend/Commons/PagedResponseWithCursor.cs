namespace FabricExplorerBackend.Commons
{
    public class PagedResponseWithCursor<T>
    {
        public T Items { get; init; }
        public PageInformationWithCursor? PageInformation { get; set; } = null!;
        //public string? ContinuationToken { get; init; }

        public PagedResponseWithCursor() { }
        public PagedResponseWithCursor(T items, PageInformationWithCursor? pageInformation)
        {
            Items = items;
            PageInformation = pageInformation;
        }
    }
}
