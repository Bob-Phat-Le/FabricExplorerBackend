using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Commons
{
    public class PageInformation
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool? HasNextPage { get; set; }
    }
}
