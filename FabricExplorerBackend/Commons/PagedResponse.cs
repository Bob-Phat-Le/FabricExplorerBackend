namespace FabricExplorerBackend.Commons
{
    public class PagedResponse<T>
    {
        public IReadOnlyList<T> Items { get; init; } = null!;
        public PageInformation PageInformation { get; set; } = null!;
        //public string? ContinuationToken { get; init; }
    }
}
